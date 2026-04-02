using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Workers;

internal sealed class WorkerQueryService : IWorkerQueryService
{
    private readonly IWorkerProfileRepository workerProfileRepository;

    public WorkerQueryService(IWorkerProfileRepository workerProfileRepository)
    {
        this.workerProfileRepository = workerProfileRepository;
    }

    public async Task<WorkerProfilesQueryResult> GetWorkersAsync(
        AuthenticatedPrincipal principal,
        GetWorkersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return WorkerProfilesQueryResult.Failure(
                "Role is not authorized to query worker profiles.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var page = request.Page ?? 1;
        var pageSize = request.PageSize ?? 20;
        var specification = new WorkerProfileQuerySpecification(
            TenantId: principal.TenantId,
            SearchText: request.SearchText,
            IncludeInactive: request.IncludeInactive ?? false,
            Page: new PageSpecification((page - 1) * pageSize, pageSize),
            SortBy: WorkerProfileSortField.DisplayName,
            SortDirection: SortDirection.Ascending);

        var items = await this.workerProfileRepository.QueryAsync(specification, cancellationToken);
        var total = await this.workerProfileRepository.CountAsync(specification, cancellationToken);

        var payload = new QueriedWorkerProfilesPage(
            Items: items.Select(ToItem).ToArray(),
            Page: page,
            PageSize: pageSize,
            Total: total);

        return WorkerProfilesQueryResult.Success(payload);
    }

    private static QueriedWorkerProfileItem ToItem(Domain.Aggregates.WorkerProfile profile)
    {
        return new QueriedWorkerProfileItem(
            WorkerId: profile.Id,
            TenantId: profile.TenantId,
            WorkerCode: profile.WorkerCode,
            DisplayName: profile.DisplayName,
            InternalRating: profile.InternalRating,
            AvailabilityStatus: profile.AvailabilityStatus,
            IsActive: profile.IsActive,
            Skills: profile.GetSkills(),
            CreatedAtUtc: profile.CreatedAtUtc,
            UpdatedAtUtc: profile.UpdatedAtUtc);
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }
}
