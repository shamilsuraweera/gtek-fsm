using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class SubscriptionRepository : EfRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Subscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == subscriptionId, cancellationToken);
    }

    public Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;

        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .Where(x => !x.EndsOnUtc.HasValue || x.EndsOnUtc.Value >= utcNow)
            .OrderByDescending(x => x.StartsOnUtc)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .OrderByDescending(x => x.StartsOnUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Subscription>> QueryAsync(SubscriptionQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (specification.ActiveOnly)
        {
            var utcNow = DateTime.UtcNow;
            query = query.Where(x => !x.EndsOnUtc.HasValue || x.EndsOnUtc.Value >= utcNow);
        }

        if (!string.IsNullOrWhiteSpace(specification.PlanCode))
        {
            query = query.Where(x => x.PlanCode == specification.PlanCode);
        }

        query = ApplySorting(query, specification.SortBy, specification.SortDirection);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Subscription> ApplySorting(
        IQueryable<Subscription> query,
        SubscriptionSortField sortBy,
        SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (SubscriptionSortField.EndsOnUtc, SortDirection.Ascending) => query.OrderBy(x => x.EndsOnUtc).ThenBy(x => x.Id),
            (SubscriptionSortField.EndsOnUtc, SortDirection.Descending) => query.OrderByDescending(x => x.EndsOnUtc).ThenByDescending(x => x.Id),
            (SubscriptionSortField.PlanCode, SortDirection.Ascending) => query.OrderBy(x => x.PlanCode).ThenBy(x => x.Id),
            (SubscriptionSortField.PlanCode, SortDirection.Descending) => query.OrderByDescending(x => x.PlanCode).ThenByDescending(x => x.Id),
            (SubscriptionSortField.StartsOnUtc, SortDirection.Ascending) => query.OrderBy(x => x.StartsOnUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.StartsOnUtc).ThenByDescending(x => x.Id),
        };
    }

    private static IQueryable<Subscription> ApplyTenantFilter(IQueryable<Subscription> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
