using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository : EfRepository<ServiceCategory>, ICategoryRepository
{
    public CategoryRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<ServiceCategory?> GetByIdAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
    }

    public Task<ServiceCategory?> GetForUpdateAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == categoryId, cancellationToken);
    }

    public Task<ServiceCategory?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategory>> ListByTenantAsync(Guid tenantId, bool includeDisabled, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId);
        if (!includeDisabled)
        {
            query = query.Where(x => x.IsEnabled);
        }

        return await query
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ServiceCategory>> ListActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .Where(x => x.IsEnabled)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<ServiceCategory> ApplyTenantFilter(IQueryable<ServiceCategory> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}