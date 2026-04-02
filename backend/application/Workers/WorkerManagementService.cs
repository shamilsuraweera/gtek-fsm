using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Workers;

internal sealed class WorkerManagementService : IWorkerManagementService
{
    private readonly IWorkerProfileRepository workerProfileRepository;
    private readonly IUnitOfWork unitOfWork;
    private readonly IAuditLogWriter auditLogWriter;

    public WorkerManagementService(
        IWorkerProfileRepository workerProfileRepository,
        IUnitOfWork unitOfWork,
        IAuditLogWriter auditLogWriter)
    {
        this.workerProfileRepository = workerProfileRepository;
        this.unitOfWork = unitOfWork;
        this.auditLogWriter = auditLogWriter;
    }

    public async Task<WorkerMutationResult> CreateAsync(
        AuthenticatedPrincipal principal,
        CreateWorkerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return WorkerMutationResult.Failure("Role is not authorized to manage workers.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var normalizedWorkerCode = request.WorkerCode?.Trim().ToUpperInvariant() ?? string.Empty;
        var duplicate = await this.workerProfileRepository.GetByCodeAsync(principal.TenantId, normalizedWorkerCode, cancellationToken);
        if (duplicate is not null)
        {
            return WorkerMutationResult.Failure("workerCode already exists for tenant.", "WORKER_CODE_CONFLICT", 409);
        }

        WorkerProfile worker;
        try
        {
            worker = new WorkerProfile(
                id: Guid.NewGuid(),
                tenantId: principal.TenantId,
                workerCode: normalizedWorkerCode,
                displayName: request.DisplayName ?? string.Empty,
                internalRating: request.InternalRating ?? 3.0m,
                skills: request.Skills ?? Array.Empty<string>(),
                baseLatitude: request.BaseLatitude,
                baseLongitude: request.BaseLongitude);

            worker.SetAvailability(ParseAvailabilityStatus(request.AvailabilityStatus));
            if (request.IsActive.HasValue && !request.IsActive.Value)
            {
                worker.Deactivate();
            }
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
        {
            return WorkerMutationResult.Failure(ex.Message, "VALIDATION_FAILED", 400);
        }

        await this.workerProfileRepository.AddAsync(worker, cancellationToken);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, worker.Id, "WORKER_PROFILE_CREATED", cancellationToken);

        return WorkerMutationResult.Success(ToItem(worker), "Worker profile created.");
    }

    public async Task<WorkerMutationResult> UpdateAsync(
        AuthenticatedPrincipal principal,
        Guid workerId,
        UpdateWorkerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return WorkerMutationResult.Failure("Role is not authorized to manage workers.", "AUTH_FORBIDDEN_ROLE", 403);
        }

        var worker = await this.workerProfileRepository.GetForUpdateAsync(principal.TenantId, workerId, cancellationToken);
        if (worker is null)
        {
            return WorkerMutationResult.Failure("Worker profile was not found.", "WORKER_NOT_FOUND", 404);
        }

        var nextWorkerCode = request.WorkerCode?.Trim().ToUpperInvariant() ?? worker.WorkerCode;
        var duplicate = await this.workerProfileRepository.GetByCodeAsync(principal.TenantId, nextWorkerCode, cancellationToken);
        if (duplicate is not null && duplicate.Id != worker.Id)
        {
            return WorkerMutationResult.Failure("workerCode already exists for tenant.", "WORKER_CODE_CONFLICT", 409);
        }

        try
        {
            worker.UpdateProfile(
                workerCode: nextWorkerCode,
                displayName: request.DisplayName ?? worker.DisplayName,
                internalRating: request.InternalRating ?? worker.InternalRating);

            if (request.Skills is not null)
            {
                worker.ReplaceSkills(request.Skills);
            }

            if (request.BaseLatitude.HasValue || request.BaseLongitude.HasValue)
            {
                worker.SetBaseLocation(request.BaseLatitude, request.BaseLongitude);
            }

            if (!string.IsNullOrWhiteSpace(request.AvailabilityStatus))
            {
                worker.SetAvailability(ParseAvailabilityStatus(request.AvailabilityStatus));
            }

            if (request.IsActive.HasValue)
            {
                if (request.IsActive.Value)
                {
                    worker.Activate();
                }
                else
                {
                    worker.Deactivate();
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException || ex is ArgumentOutOfRangeException)
        {
            return WorkerMutationResult.Failure(ex.Message, "VALIDATION_FAILED", 400);
        }

        this.workerProfileRepository.Update(worker);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);
        await WriteAuditAsync(principal, worker.Id, "WORKER_PROFILE_UPDATED", cancellationToken);

        return WorkerMutationResult.Success(ToItem(worker), "Worker profile updated.");
    }

    private static QueriedWorkerProfileItem ToItem(WorkerProfile worker)
    {
        return new QueriedWorkerProfileItem(
            WorkerId: worker.Id,
            TenantId: worker.TenantId,
            WorkerCode: worker.WorkerCode,
            DisplayName: worker.DisplayName,
            InternalRating: worker.InternalRating,
            AvailabilityStatus: worker.AvailabilityStatus,
            IsActive: worker.IsActive,
            Skills: worker.GetSkills(),
            BaseLatitude: worker.BaseLatitude,
            BaseLongitude: worker.BaseLongitude,
            CreatedAtUtc: worker.CreatedAtUtc,
            UpdatedAtUtc: worker.UpdatedAtUtc);
    }

    private async Task WriteAuditAsync(
        AuthenticatedPrincipal principal,
        Guid workerId,
        string action,
        CancellationToken cancellationToken)
    {
        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = principal.UserId,
            TenantId = principal.TenantId,
            EntityType = "WorkerProfile",
            EntityId = workerId,
            Action = action,
            Outcome = "Success",
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Details = null,
        };

        await this.auditLogWriter.WriteAsync(audit, cancellationToken);
    }

    private static WorkerAvailabilityStatus ParseAvailabilityStatus(string? availabilityStatus)
    {
        if (string.IsNullOrWhiteSpace(availabilityStatus))
        {
            return WorkerAvailabilityStatus.Available;
        }

        if (Enum.TryParse<WorkerAvailabilityStatus>(availabilityStatus, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        throw new ArgumentException("availabilityStatus is invalid.", nameof(availabilityStatus));
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
