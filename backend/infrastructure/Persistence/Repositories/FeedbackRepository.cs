using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class FeedbackRepository : EfRepository<Feedback>, IFeedbackRepository
{
    private readonly GtekFsmDbContext dbContext;

    public FeedbackRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<Feedback?> GetByIdAsync(Guid tenantId, Guid feedbackId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.Id == feedbackId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Feedback>> GetByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.ServiceRequestId == serviceRequestId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Feedback>> GetByJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.JobId == jobId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Feedback>> GetByProvidedByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.ProvidedByUserId == userId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Feedback>> QueryAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId)
            .CountAsync(cancellationToken);
    }

    public async Task<decimal> GetAverageRatingForServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default)
    {
        var average = await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.ServiceRequestId == serviceRequestId && f.Rating > 0)
            .AverageAsync(f => (decimal?)f.Rating, cancellationToken);

        return average ?? 0m;
    }

    public async Task<decimal> GetAverageRatingForJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var average = await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.JobId == jobId && f.Rating > 0)
            .AverageAsync(f => (decimal?)f.Rating, cancellationToken);

        return average ?? 0m;
    }

    public async Task<IReadOnlyList<Feedback>> GetActionableFeedbackAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId && f.IsActionable)
            .OrderByDescending(f => f.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<int, int>> GetFeedbackCountBySourceAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await this.Queryable()
            .Where(f => f.TenantId == tenantId)
            .GroupBy(f => (int)f.Source)
            .Select(g => new { Source = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Source, x => x.Count, cancellationToken);
    }
}
