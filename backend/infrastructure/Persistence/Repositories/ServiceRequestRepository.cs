using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class ServiceRequestRepository : EfRepository<ServiceRequest>, IServiceRequestRepository
{
    public ServiceRequestRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<ServiceRequest?> GetByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .Where(x => x.CustomerUserId == customerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceRequest>> QueryAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (specification.CustomerUserId.HasValue)
        {
            query = query.Where(x => x.CustomerUserId == specification.CustomerUserId.Value);
        }

        if (specification.Status.HasValue)
        {
            query = query.Where(x => x.Status == specification.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(specification.SearchText))
        {
            query = query.Where(x => x.Title.Contains(specification.SearchText));
        }

        query = ApplySorting(query, specification.SortBy, specification.SortDirection);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<ServiceRequest> ApplySorting(
        IQueryable<ServiceRequest> query,
        ServiceRequestSortField sortBy,
        SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (ServiceRequestSortField.Status, SortDirection.Ascending) => query.OrderBy(x => x.Status).ThenBy(x => x.Id),
            (ServiceRequestSortField.Status, SortDirection.Descending) => query.OrderByDescending(x => x.Status).ThenByDescending(x => x.Id),
            (ServiceRequestSortField.Title, SortDirection.Ascending) => query.OrderBy(x => x.Title).ThenBy(x => x.Id),
            (ServiceRequestSortField.Title, SortDirection.Descending) => query.OrderByDescending(x => x.Title).ThenByDescending(x => x.Id),
            (ServiceRequestSortField.CreatedAtUtc, SortDirection.Ascending) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id),
        };
    }

    private static IQueryable<ServiceRequest> ApplyTenantFilter(IQueryable<ServiceRequest> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
