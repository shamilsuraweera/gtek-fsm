using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Audit;
using System.Text.Json;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestAssignmentService : IServiceRequestAssignmentService
{
    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IJobRepository jobRepository;
    private readonly IUserRepository userRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;
    private readonly IOperationalUpdatePublisher operationalUpdatePublisher;
    private readonly ServiceRequestSlaOptions slaOptions = new();

    public ServiceRequestAssignmentService(
        IServiceRequestRepository serviceRequestRepository,
        IJobRepository jobRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter,
        IOperationalUpdatePublisher operationalUpdatePublisher)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.jobRepository = jobRepository;
        this.userRepository = userRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
        this.operationalUpdatePublisher = operationalUpdatePublisher;
    }

    public async Task<ServiceRequestAssignmentResult> AssignAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? workerUserId,
        string? rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseWorker(workerUserId, out var parsedWorkerId))
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Worker user id is required and must be a valid guid.",
                errorCode: "VALIDATION_WORKER_ID_INVALID",
                statusCode: 400);
        }

        var worker = await this.userRepository.GetByIdAsync(principal.TenantId, parsedWorkerId, cancellationToken);
        if (worker is null)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Worker user was not found within tenant scope.",
                errorCode: "WORKER_NOT_FOUND_IN_TENANT",
                statusCode: 404);
        }

        var request = await this.serviceRequestRepository.GetForUpdateAsync(principal.TenantId, requestId, cancellationToken);
        if (request is null)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Service request was not found.",
                errorCode: "REQUEST_NOT_FOUND",
                statusCode: 404);
        }

        if (!TryValidateRowVersion(rowVersion, request.RowVersion, out var validationErrorCode, out var validationMessage))
        {
            return ServiceRequestAssignmentResult.Failure(
                message: validationMessage,
                errorCode: validationErrorCode,
                statusCode: 409);
        }

        if (request.ActiveJobId.HasValue)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Service request already has an active assignment.",
                errorCode: "REQUEST_ASSIGNMENT_ALREADY_EXISTS",
                statusCode: 409);
        }

        await using var tx = await this.unitOfWork.BeginTransactionAsync(cancellationToken);

        var previousResponseSlaState = request.ResponseSlaState;
        var previousAssignmentSlaState = request.AssignmentSlaState;
        var previousCompletionSlaState = request.CompletionSlaState;

        try
        {
            if (request.Status == ServiceRequestStatus.New)
            {
                request.TransitionTo(ServiceRequestStatus.Assigned);
            }

            if (request.Status != ServiceRequestStatus.Assigned)
            {
                await tx.RollbackAsync(cancellationToken);
                return ServiceRequestAssignmentResult.Failure(
                    message: "Request must be in Assigned status to assign a worker.",
                    errorCode: "REQUEST_ASSIGNMENT_INVALID_STATE",
                    statusCode: 400);
            }

            var job = new Job(Guid.NewGuid(), request.TenantId, request.Id);
            job.AssignWorker(parsedWorkerId);
            request.LinkJob(job.Id);

            var snapshot = ServiceRequestSlaCalculator.Compute(
                request,
                assignmentStatus: job.AssignmentStatus,
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

            await this.jobRepository.AddAsync(job, cancellationToken);
            this.serviceRequestRepository.Update(request);

            try
            {
                await this.unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                await tx.RollbackAsync(cancellationToken);
                return ServiceRequestAssignmentResult.Failure(
                    message: "The request was modified by another operation. Refresh and retry.",
                    errorCode: "CONCURRENCY_CONFLICT",
                    statusCode: 409);
            }

            await tx.CommitAsync(cancellationToken);

            // Write audit log
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = principal.UserId,
                TenantId = principal.TenantId,
                EntityType = "ServiceRequest",
                EntityId = request.Id,
                Action = $"AssignWorker:{parsedWorkerId}",
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

            var payload = BuildPayload(request, job, previousWorkerUserId: null, parsedWorkerId);
            await this.operationalUpdatePublisher.PublishJobAssignmentUpdatedAsync(payload, cancellationToken);

            return ServiceRequestAssignmentResult.Success(
                payload: payload,
                message: "Service request assigned successfully.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return ServiceRequestAssignmentResult.Failure(
                message: ex.Message,
                errorCode: "REQUEST_ASSIGNMENT_INVALID",
                statusCode: 400);
        }
    }

    public async Task<ServiceRequestAssignmentResult> ReassignAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? workerUserId,
        string? rowVersion,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseWorker(workerUserId, out var parsedWorkerId))
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Worker user id is required and must be a valid guid.",
                errorCode: "VALIDATION_WORKER_ID_INVALID",
                statusCode: 400);
        }

        var worker = await this.userRepository.GetByIdAsync(principal.TenantId, parsedWorkerId, cancellationToken);
        if (worker is null)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Worker user was not found within tenant scope.",
                errorCode: "WORKER_NOT_FOUND_IN_TENANT",
                statusCode: 404);
        }

        var request = await this.serviceRequestRepository.GetForUpdateAsync(principal.TenantId, requestId, cancellationToken);
        if (request is null)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Service request was not found.",
                errorCode: "REQUEST_NOT_FOUND",
                statusCode: 404);
        }

        if (!TryValidateRowVersion(rowVersion, request.RowVersion, out var validationErrorCode, out var validationMessage))
        {
            return ServiceRequestAssignmentResult.Failure(
                message: validationMessage,
                errorCode: validationErrorCode,
                statusCode: 409);
        }

        if (!request.ActiveJobId.HasValue)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Service request does not have an active assignment to reassign.",
                errorCode: "REQUEST_REASSIGNMENT_NO_ACTIVE_ASSIGNMENT",
                statusCode: 400);
        }

        var job = await this.jobRepository.GetForUpdateAsync(principal.TenantId, request.ActiveJobId.Value, cancellationToken);
        if (job is null)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Active job linked to service request was not found.",
                errorCode: "JOB_NOT_FOUND",
                statusCode: 404);
        }

        var previousWorkerUserId = job.AssignedWorkerUserId;
        var previousResponseSlaState = request.ResponseSlaState;
        var previousAssignmentSlaState = request.AssignmentSlaState;
        var previousCompletionSlaState = request.CompletionSlaState;

        if (!previousWorkerUserId.HasValue)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Active assignment has no worker to replace.",
                errorCode: "REQUEST_REASSIGNMENT_INVALID",
                statusCode: 400);
        }

        if (previousWorkerUserId.Value == parsedWorkerId)
        {
            return ServiceRequestAssignmentResult.Failure(
                message: "Reassignment requires a different worker.",
                errorCode: "REQUEST_REASSIGNMENT_SAME_WORKER",
                statusCode: 400);
        }

        await using var tx = await this.unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            if (request.Status != ServiceRequestStatus.Assigned)
            {
                await tx.RollbackAsync(cancellationToken);
                return ServiceRequestAssignmentResult.Failure(
                    message: "Request must be in Assigned status to reassign worker.",
                    errorCode: "REQUEST_REASSIGNMENT_INVALID_STATE",
                    statusCode: 400);
            }

            job.MarkCancelled();
            job.UnassignWorker();
            job.AssignWorker(parsedWorkerId);

            var snapshot = ServiceRequestSlaCalculator.Compute(
                request,
                assignmentStatus: job.AssignmentStatus,
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

            this.jobRepository.Update(job);
            this.serviceRequestRepository.Update(request);

            try
            {
                await this.unitOfWork.SaveChangesAsync(cancellationToken);
            }
            catch (ConcurrencyConflictException)
            {
                await tx.RollbackAsync(cancellationToken);
                return ServiceRequestAssignmentResult.Failure(
                    message: "The request was modified by another operation. Refresh and retry.",
                    errorCode: "CONCURRENCY_CONFLICT",
                    statusCode: 409);
            }

            await tx.CommitAsync(cancellationToken);

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

            var payload = BuildPayload(request, job, previousWorkerUserId, parsedWorkerId);
            await this.operationalUpdatePublisher.PublishJobAssignmentUpdatedAsync(payload, cancellationToken);

            return ServiceRequestAssignmentResult.Success(
                payload: payload,
                message: "Service request reassigned successfully.");
        }
        catch (InvalidOperationException ex)
        {
            await tx.RollbackAsync(cancellationToken);
            return ServiceRequestAssignmentResult.Failure(
                message: ex.Message,
                errorCode: "REQUEST_REASSIGNMENT_INVALID",
                statusCode: 400);
        }
    }

    private static bool TryParseWorker(string? workerUserId, out Guid parsedWorkerId)
    {
        parsedWorkerId = Guid.Empty;
        return !string.IsNullOrWhiteSpace(workerUserId)
            && Guid.TryParse(workerUserId.Trim(), out parsedWorkerId)
            && parsedWorkerId != Guid.Empty;
    }

    private static AssignedServiceRequestPayload BuildPayload(
        ServiceRequest request,
        Job job,
        Guid? previousWorkerUserId,
        Guid currentWorkerUserId)
    {
        return new AssignedServiceRequestPayload(
            RequestId: request.Id,
            TenantId: request.TenantId,
            JobId: job.Id,
            PreviousWorkerUserId: previousWorkerUserId,
            CurrentWorkerUserId: currentWorkerUserId,
            AssignmentStatus: job.AssignmentStatus.ToString(),
            UpdatedAtUtc: job.UpdatedAtUtc,
            RowVersion: Convert.ToBase64String(request.RowVersion));
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
