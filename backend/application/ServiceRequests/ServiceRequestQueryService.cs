using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestQueryService : IServiceRequestQueryService
{
    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IJobRepository jobRepository;

    public ServiceRequestQueryService(
        IServiceRequestRepository serviceRequestRepository,
        IJobRepository jobRepository)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.jobRepository = jobRepository;
    }

    public async Task<ServiceRequestQueryResult> QueryAsync(
        AuthenticatedPrincipal principal,
        GetRequestsRequest request,
        CancellationToken cancellationToken = default)
    {
        var normalizedRequest = request ?? new GetRequestsRequest();

        if (normalizedRequest.CreatedFromUtc.HasValue
            && normalizedRequest.CreatedToUtc.HasValue
            && normalizedRequest.CreatedFromUtc.Value > normalizedRequest.CreatedToUtc.Value)
        {
            return ServiceRequestQueryResult.Failure(
                message: "createdFromUtc cannot be greater than createdToUtc.",
                errorCode: "VALIDATION_DATE_RANGE_INVALID",
                statusCode: 400);
        }

        Guid? scopedCustomerUserId = null;
        Guid? scopedAssignedWorkerUserId = null;

        if (principal.IsInRole("Customer"))
        {
            scopedCustomerUserId = principal.UserId;
        }
        else if (principal.IsInRole("Worker"))
        {
            scopedAssignedWorkerUserId = principal.UserId;
        }
        else if (!principal.IsInRole("Support") && !principal.IsInRole("Manager") && !principal.IsInRole("Admin"))
        {
            return ServiceRequestQueryResult.Failure(
                message: "Role is not authorized to query requests.",
                errorCode: "AUTH_FORBIDDEN_ROLE",
                statusCode: 403);
        }

        if (!string.IsNullOrWhiteSpace(normalizedRequest.AssignedWorkerUserIdFilter))
        {
            if (!Guid.TryParse(normalizedRequest.AssignedWorkerUserIdFilter.Trim(), out var parsedWorkerUserId)
                || parsedWorkerUserId == Guid.Empty)
            {
                return ServiceRequestQueryResult.Failure(
                    message: "assignedWorkerUserIdFilter must be a valid GUID.",
                    errorCode: "VALIDATION_ASSIGNED_WORKER_ID_INVALID",
                    statusCode: 400);
            }

            if (scopedAssignedWorkerUserId.HasValue && scopedAssignedWorkerUserId.Value != parsedWorkerUserId)
            {
                return ServiceRequestQueryResult.Failure(
                    message: "Workers can only query requests assigned to themselves.",
                    errorCode: "AUTH_FORBIDDEN_ROLE",
                    statusCode: 403);
            }

            scopedAssignedWorkerUserId = parsedWorkerUserId;
        }

        ServiceRequestStatus? status = null;
        if (!string.IsNullOrWhiteSpace(normalizedRequest.StatusFilter))
        {
            if (!Enum.TryParse<ServiceRequestStatus>(normalizedRequest.StatusFilter.Trim(), true, out var parsedStatus))
            {
                return ServiceRequestQueryResult.Failure(
                    message: "statusFilter is invalid.",
                    errorCode: "VALIDATION_STATUS_FILTER_INVALID",
                    statusCode: 400);
            }

            status = parsedStatus;
        }

        var sortBy = ParseSortField(normalizedRequest.SortBy);
        var sortDirection = ParseSortDirection(normalizedRequest.SortDirection);
        var page = new PageSpecification(normalizedRequest.Page ?? 1, normalizedRequest.PageSize ?? 25);

        var specification = new ServiceRequestQuerySpecification(
            TenantId: principal.TenantId,
            CustomerUserId: scopedCustomerUserId,
            Status: status,
            CreatedFromUtc: normalizedRequest.CreatedFromUtc,
            CreatedToUtc: normalizedRequest.CreatedToUtc,
            AssignedWorkerUserId: scopedAssignedWorkerUserId,
            SearchText: normalizedRequest.SearchText,
            Page: page,
            SortBy: sortBy,
            SortDirection: sortDirection);

        var items = await this.serviceRequestRepository.QueryAsync(specification, cancellationToken);
        var total = await this.serviceRequestRepository.CountAsync(specification, cancellationToken);

        var projectedItems = new List<QueriedServiceRequestItem>(items.Count);
        foreach (var item in items)
        {
            Guid? assignedWorkerUserId = null;
            if (item.ActiveJobId.HasValue)
            {
                var activeJob = await this.jobRepository.GetByIdAsync(principal.TenantId, item.ActiveJobId.Value, cancellationToken);
                assignedWorkerUserId = activeJob?.AssignedWorkerUserId;
            }

            projectedItems.Add(new QueriedServiceRequestItem(
                RequestId: item.Id,
                Status: item.Status.ToString(),
                Summary: item.Title,
                TenantId: item.TenantId,
                CustomerUserId: item.CustomerUserId,
                CreatedAtUtc: item.CreatedAtUtc,
                UpdatedAtUtc: item.UpdatedAtUtc,
                ActiveJobId: item.ActiveJobId,
                AssignedWorkerUserId: assignedWorkerUserId));
        }

        return ServiceRequestQueryResult.Success(
            new QueriedServiceRequestPage(
                Items: projectedItems,
                Page: page.NormalizedPageNumber,
                PageSize: page.NormalizedPageSize,
                Total: total));
    }

    private static ServiceRequestSortField ParseSortField(string? sortBy)
    {
        if (string.IsNullOrWhiteSpace(sortBy))
        {
            return ServiceRequestSortField.CreatedAtUtc;
        }

        return sortBy.Trim().ToLowerInvariant() switch
        {
            "status" => ServiceRequestSortField.Status,
            "title" => ServiceRequestSortField.Title,
            "summary" => ServiceRequestSortField.Title,
            _ => ServiceRequestSortField.CreatedAtUtc,
        };
    }

    private static SortDirection ParseSortDirection(string? direction)
    {
        return string.Equals(direction?.Trim(), "asc", StringComparison.OrdinalIgnoreCase)
            ? SortDirection.Ascending
            : SortDirection.Descending;
    }
}
