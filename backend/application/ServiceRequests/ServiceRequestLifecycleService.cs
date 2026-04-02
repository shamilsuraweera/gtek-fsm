using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Audit;
using System.Text.Json;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestLifecycleService : IServiceRequestLifecycleService
{
    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly IOperationalUpdatePublisher operationalUpdatePublisher;
    private readonly ServiceRequestSlaOptions slaOptions = new();

    public ServiceRequestLifecycleService(
        IServiceRequestRepository serviceRequestRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter,
        IOperationalUpdatePublisher operationalUpdatePublisher)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
        this.operationalUpdatePublisher = operationalUpdatePublisher;
    }

    public async Task<TransitionServiceRequestResult> TransitionAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? nextStatus,
        string? rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nextStatus))
        {
            return TransitionServiceRequestResult.Failure(
                message: "Next status is required.",
                errorCode: "VALIDATION_NEXT_STATUS_REQUIRED",
                statusCode: 400);
        }

        if (!Enum.TryParse<ServiceRequestStatus>(nextStatus.Trim(), ignoreCase: true, out var parsedNextStatus))
        {
            return TransitionServiceRequestResult.Failure(
                message: "Requested status is invalid.",
                errorCode: "VALIDATION_NEXT_STATUS_INVALID",
                statusCode: 400);
        }

        var request = await this.serviceRequestRepository.GetForUpdateAsync(principal.TenantId, requestId, cancellationToken);
        if (request is null)
        {
            return TransitionServiceRequestResult.Failure(
                message: "Service request was not found.",
                errorCode: "REQUEST_NOT_FOUND",
                statusCode: 404);
        }

        if (!TryValidateRowVersion(rowVersion, request.RowVersion, out var validationErrorCode, out var validationMessage))
        {
            return TransitionServiceRequestResult.Failure(
                message: validationMessage,
                errorCode: validationErrorCode,
                statusCode: 409);
        }

        var previousStatus = request.Status;
        var previousResponseSlaState = request.ResponseSlaState;
        var previousAssignmentSlaState = request.AssignmentSlaState;
        var previousCompletionSlaState = request.CompletionSlaState;

        try
        {
            request.TransitionTo(parsedNextStatus);
        }
        catch (InvalidOperationException ex)
        {
            return TransitionServiceRequestResult.Failure(
                message: ex.Message,
                errorCode: "REQUEST_TRANSITION_INVALID",
                statusCode: 400);
        }

        var snapshot = ServiceRequestSlaCalculator.Compute(
            request,
            assignmentStatus: null,
            nowUtc: DateTime.UtcNow,
            options: this.slaOptions);

        var escalations = ServiceRequestSlaEscalationEvaluator.Evaluate(
            previousResponseSlaState,
            previousAssignmentSlaState,
            previousCompletionSlaState,
            snapshot);

        request.ApplySlaSnapshot(
            snapshot.ResponseDueAtUtc,
            snapshot.AssignmentDueAtUtc,
            snapshot.CompletionDueAtUtc,
            snapshot.ResponseSlaState,
            snapshot.AssignmentSlaState,
            snapshot.CompletionSlaState,
            snapshot.NextSlaDeadlineAtUtc);

        this.serviceRequestRepository.Update(request);
        try
        {
            await this.unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return TransitionServiceRequestResult.Failure(
                message: "The request was modified by another operation. Refresh and retry.",
                errorCode: "CONCURRENCY_CONFLICT",
                statusCode: 409);
        }

        // Write audit log
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = principal.UserId,
            TenantId = principal.TenantId,
            EntityType = "ServiceRequest",
            EntityId = request.Id,
            Action = $"StatusTransition:{previousStatus}->{request.Status}",
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = null,
        };
        await this.auditLogWriter.WriteAsync(auditLog, cancellationToken);

        foreach (var escalation in escalations)
        {
            var escalationAudit = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = null,
                TenantId = principal.TenantId,
                EntityType = "ServiceRequest",
                EntityId = request.Id,
                Action = $"SlaEscalation:{escalation.SlaDimension}:{escalation.CurrentState}",
                Outcome = "Success",
                OccurredAtUtc = DateTimeOffset.UtcNow,
                Details = JsonSerializer.Serialize(new
                {
                    escalation.SlaDimension,
                    PreviousState = escalation.PreviousState.ToString(),
                    CurrentState = escalation.CurrentState.ToString(),
                    escalation.DueAtUtc,
                    TriggeredByUserId = principal.UserId,
                }),
            };

            await this.auditLogWriter.WriteAsync(escalationAudit, cancellationToken);

            var escalationPayload = new SlaEscalationTriggeredPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                SlaDimension: escalation.SlaDimension,
                PreviousSlaStatus: escalation.PreviousState.ToString(),
                CurrentSlaStatus: escalation.CurrentState.ToString(),
                DueAtUtc: escalation.DueAtUtc,
                TriggeredAtUtc: DateTime.UtcNow,
                RowVersion: Convert.ToBase64String(request.RowVersion));

            await this.operationalUpdatePublisher.PublishSlaEscalationTriggeredAsync(escalationPayload, cancellationToken);
        }

        var payload = new TransitionedServiceRequestPayload(
            RequestId: request.Id,
            TenantId: request.TenantId,
            PreviousStatus: previousStatus.ToString(),
            CurrentStatus: request.Status.ToString(),
            UpdatedAtUtc: request.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(request.RowVersion));

        await this.operationalUpdatePublisher.PublishServiceRequestStatusUpdatedAsync(payload, cancellationToken);

        return TransitionServiceRequestResult.Success(payload);
    }

    private static bool TryValidateRowVersion(
        string? requestRowVersion,
        byte[] currentRowVersion,
        out string errorCode,
        out string message)
    {
        errorCode = string.Empty;
        message = string.Empty;

        if (string.IsNullOrWhiteSpace(requestRowVersion))
        {
            return true;
        }

        byte[] decoded;
        try
        {
            decoded = Convert.FromBase64String(requestRowVersion.Trim());
        }
        catch (FormatException)
        {
            errorCode = "ROW_VERSION_INVALID";
            message = "rowVersion must be a valid base64 string.";
            return false;
        }

        if (!decoded.AsSpan().SequenceEqual(currentRowVersion))
        {
            errorCode = "CONCURRENCY_CONFLICT";
            message = "The request was modified by another operation. Refresh and retry.";
            return false;
        }

        return true;
    }
}
