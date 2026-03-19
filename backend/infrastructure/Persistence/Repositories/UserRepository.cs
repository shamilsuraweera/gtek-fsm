using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class UserRepository : EfRepository<User>, IUserRepository
{
    public UserRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);
    }

    public Task<User?> GetByExternalIdentityAsync(Guid tenantId, string externalIdentity, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.ExternalIdentity == externalIdentity, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<User>> QueryAsync(UserQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (!string.IsNullOrWhiteSpace(specification.SearchText))
        {
            query = query.Where(x => x.DisplayName.Contains(specification.SearchText));
        }

        if (!string.IsNullOrWhiteSpace(specification.ExternalIdentity))
        {
            query = query.Where(x => x.ExternalIdentity == specification.ExternalIdentity);
        }

        query = ApplySorting(query, specification.SortBy, specification.SortDirection);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<User> ApplySorting(IQueryable<User> query, UserSortField sortBy, SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (UserSortField.CreatedAtUtc, SortDirection.Ascending) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            (UserSortField.CreatedAtUtc, SortDirection.Descending) => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id),
            (UserSortField.DisplayName, SortDirection.Descending) => query.OrderByDescending(x => x.DisplayName).ThenByDescending(x => x.Id),
            _ => query.OrderBy(x => x.DisplayName).ThenBy(x => x.Id),
        };
    }

    private static IQueryable<User> ApplyTenantFilter(IQueryable<User> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
