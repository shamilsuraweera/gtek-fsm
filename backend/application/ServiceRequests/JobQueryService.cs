using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class JobQueryService : IJobQueryService
{
    private readonly IJobRepository jobRepository;

    public JobQueryService(IJobRepository jobRepository)
    {
        this.jobRepository = jobRepository;
    }

    public async Task<JobQueryResult> QueryAsync(
        AuthenticatedPrincipal principal,
        GetJobsRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = request ?? new GetJobsRequest();

        if (normalizedRequest.ScheduledFromUtc.HasValue
            && normalizedRequest.ScheduledToUtc.HasValue
            && normalizedRequest.ScheduledFromUtc.Value > normalizedRequest.ScheduledToUtc.Value)
        {
            return JobQueryResult.Failure(
                message: "scheduledFromUtc cannot be greater than scheduledToUtc.",
                errorCode: "VALIDATION_DATE_RANGE_INVALID",
                statusCode: 400);
        }

        Guid? scopedWorkerUserId = null;

        if (principal.IsInRole("Worker"))
        {
            scopedWorkerUserId = principal.UserId;
        }
        else if (principal.IsInRole("Customer"))
        {
            return JobQueryResult.Failure(
                message: "Customers are not allowed to query jobs.",
                errorCode: "AUTH_FORBIDDEN_ROLE",
                statusCode: 403);
        }
        else if (!principal.IsInRole("Support") && !principal.IsInRole("Manager") && !principal.IsInRole("Admin"))
        {
            return JobQueryResult.Failure(
                message: "Role is not authorized to query jobs.",
                errorCode: "AUTH_FORBIDDEN_ROLE",
                statusCode: 403);
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequest.WorkerIdFilter))
        {
            if (!Guid.TryParse(normalizedRequest.WorkerIdFilter.Trim(), out var parsedWorkerUserId)
                || parsedWorkerUserId == Guid.Empty)
            {
                return JobQueryResult.Failure(
                    message: "workerIdFilter must be a valid GUID.",
                    errorCode: "VALIDATION_WORKER_FILTER_INVALID",
                    statusCode: 400);
            }

            if (scopedWorkerUserId.HasValue && scopedWorkerUserId.Value != parsedWorkerUserId)
            {
                return JobQueryResult.Failure(
                    message: "Workers can only query their own jobs.",
                    errorCode: "AUTH_FORBIDDEN_ROLE",
                    statusCode: 403);
            }

            scopedWorkerUserId = parsedWorkerUserId;
        }

        AssignmentStatus? assignmentStatus = null;
        if (!string.IsNullOrWhiteSpace(normalizedRequest.StatusFilter))
        {
            if (!Enum.TryParse<AssignmentStatus>(normalizedRequest.StatusFilter.Trim(), true, out var parsedStatus))
            {
                return JobQueryResult.Failure(
                    message: "statusFilter is invalid.",
                    errorCode: "VALIDATION_STATUS_FILTER_INVALID",
                    statusCode: 400);
            }

            assignmentStatus = parsedStatus;
        }

        var sortBy = ParseSortField(normalizedRequest.SortBy);
        var sortDirection = ParseSortDirection(normalizedRequest.SortDirection);
        var page = new PageSpecification(normalizedRequest.Page ?? 1, normalizedRequest.PageSize ?? 25);

        var specification = new JobQuerySpecification(
            TenantId: principal.TenantId,
            AssignedWorkerUserId: scopedWorkerUserId,
            AssignmentStatus: assignmentStatus,
            ScheduledFromUtc: normalizedRequest.ScheduledFromUtc,
            ScheduledToUtc: normalizedRequest.ScheduledToUtc,
            SearchText: normalizedRequest.SearchText,
            Page: page,
            SortBy: sortBy,
            SortDirection: sortDirection);

        var items = await this.jobRepository.QueryAsync(specification, cancellationToken);
        var total = await this.jobRepository.CountAsync(specification, cancellationToken);

        var projectedItems = items
            .Select(x => new QueriedJobItem(
                JobId: x.Id,
                Title: $"Job {x.Id}",
                Status: x.AssignmentStatus.ToString(),
                RequestId: x.ServiceRequestId,
                AssignedTo: x.AssignedWorkerUserId,
                AssignedUtc: x.CreatedAtUtc))
            .ToArray();

        return JobQueryResult.Success(
            new QueriedJobPage(
                Items: projectedItems,
                Page: page.NormalizedPageNumber,
                PageSize: page.NormalizedPageSize,
                Total: total));
    }

    private static JobSortField ParseSortField(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return JobSortField.CreatedAtUtc;
        }

        return sortBy.Trim().ToLowerInvariant() switch
        {
            "status" => JobSortField.AssignmentStatus,
            "assignmentstatus" => JobSortField.AssignmentStatus,
            _ => JobSortField.CreatedAtUtc,
        };
    }

    private static SortDirection ParseSortDirection(string? direction)
    {
        return string.Equals(direction?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Ascending
            : SortDirection.Descending;
    }
}
