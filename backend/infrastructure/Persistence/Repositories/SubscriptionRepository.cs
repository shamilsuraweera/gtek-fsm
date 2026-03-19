using GTEK.FSM.Backend.Application.Persistence.Repositories;
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

    private static IQueryable<Subscription> ApplyTenantFilter(IQueryable<Subscription> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
