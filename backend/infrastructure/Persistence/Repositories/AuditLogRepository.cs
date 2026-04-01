using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Audit;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly GtekFsmDbContext dbContext;

    public AuditLogRepository(GtekFsmDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLog>> QueryAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(this.dbContext.AuditLogs.AsNoTracking(), specification)
            .OrderByDescending(x => x.OccurredAtUtc)
            .ThenByDescending(x => x.Id);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        return ApplyFilters(this.dbContext.AuditLogs.AsNoTracking(), specification)
            .CountAsync(cancellationToken);
    }

    private static IQueryable<AuditLog> ApplyFilters(IQueryable<AuditLog> query, AuditLogQuerySpecification specification)
    {
        query = query.Where(x => x.TenantId == specification.TenantId);

        if (specification.ActorUserId.HasValue)
        {
            query = query.Where(x => x.ActorUserId == specification.ActorUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(specification.EntityType))
        {
            query = query.Where(x => x.EntityType == specification.EntityType);
        }

        if (specification.EntityId.HasValue)
        {
            query = query.Where(x => x.EntityId == specification.EntityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(specification.Action))
        {
            query = query.Where(x => x.Action.Contains(specification.Action));
        }

        if (!string.IsNullOrWhiteSpace(specification.Outcome))
        {
            query = query.Where(x => x.Outcome == specification.Outcome);
        }

        if (specification.OccurredFromUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc >= specification.OccurredFromUtc.Value);
        }

        if (specification.OccurredToUtc.HasValue)
        {
            query = query.Where(x => x.OccurredAtUtc <= specification.OccurredToUtc.Value);
        }

        return query;
    }
}
