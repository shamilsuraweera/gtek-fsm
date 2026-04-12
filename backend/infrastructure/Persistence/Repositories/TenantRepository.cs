using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class TenantRepository : EfRepository<Tenant>, ITenantRepository
{
    public TenantRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public async Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .AsNoTracking()
            .OrderBy(x => x.Code)
            .ToListAsync(cancellationToken);
    }

    public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return this.Queryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tenantId, cancellationToken);
    }

    public Task<Tenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        return this.Queryable()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == tenantCode, cancellationToken);
    }

    public Task<bool> ExistsByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
    {
        return this.Queryable()
            .AsNoTracking()
            .AnyAsync(x => x.Code == tenantCode, cancellationToken);
    }
}
