using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class WorkerProfileRepository : EfRepository<WorkerProfile>, IWorkerProfileRepository
{
    public WorkerProfileRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<WorkerProfile?> GetByIdAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == workerId, cancellationToken);
    }

    public Task<WorkerProfile?> GetForUpdateAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == workerId, cancellationToken);
    }

    public Task<WorkerProfile?> GetByCodeAsync(Guid tenantId, string workerCode, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.WorkerCode == workerCode, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkerProfile>> QueryAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (!specification.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(specification.SearchText))
        {
            query = query.Where(x => x.DisplayName.Contains(specification.SearchText) || x.WorkerCode.Contains(specification.SearchText));
        }

        query = ApplySorting(query, specification.SortBy, specification.SortDirection);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (!specification.IncludeInactive)
        {
            query = query.Where(x => x.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(specification.SearchText))
        {
            query = query.Where(x => x.DisplayName.Contains(specification.SearchText) || x.WorkerCode.Contains(specification.SearchText));
        }

        return await query.CountAsync(cancellationToken);
    }

    private static IQueryable<WorkerProfile> ApplySorting(IQueryable<WorkerProfile> query, WorkerProfileSortField sortBy, SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (WorkerProfileSortField.CreatedAtUtc, SortDirection.Ascending) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            (WorkerProfileSortField.CreatedAtUtc, SortDirection.Descending) => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id),
            (WorkerProfileSortField.InternalRating, SortDirection.Ascending) => query.OrderBy(x => x.InternalRating).ThenBy(x => x.DisplayName),
            (WorkerProfileSortField.InternalRating, SortDirection.Descending) => query.OrderByDescending(x => x.InternalRating).ThenByDescending(x => x.DisplayName),
            (WorkerProfileSortField.DisplayName, SortDirection.Descending) => query.OrderByDescending(x => x.DisplayName).ThenByDescending(x => x.Id),
            _ => query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id),
        };
    }

    private static IQueryable<WorkerProfile> ApplyTenantFilter(IQueryable<WorkerProfile> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
