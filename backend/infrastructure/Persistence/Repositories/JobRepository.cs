using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal sealed class JobRepository : EfRepository<Job>, IJobRepository
{
    public JobRepository(GtekFsmDbContext dbContext)
        : base(dbContext)
    {
    }

    public Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        return ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .FirstOrDefaultAsync(x => x.Id == jobId, cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> ListByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .Where(x => x.ServiceRequestId == serviceRequestId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default)
    {
        return await ApplyTenantFilter(this.Queryable().AsNoTracking(), tenantId)
            .Where(x => x.AssignedWorkerUserId == workerUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Job>> QueryAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default)
    {
        var query = ApplyTenantFilter(this.Queryable().AsNoTracking(), specification.TenantId);

        if (specification.ServiceRequestId.HasValue)
        {
            query = query.Where(x => x.ServiceRequestId == specification.ServiceRequestId.Value);
        }

        if (specification.AssignedWorkerUserId.HasValue)
        {
            query = query.Where(x => x.AssignedWorkerUserId == specification.AssignedWorkerUserId.Value);
        }

        if (specification.AssignmentStatus.HasValue)
        {
            query = query.Where(x => x.AssignmentStatus == specification.AssignmentStatus.Value);
        }

        query = ApplySorting(query, specification.SortBy, specification.SortDirection);

        var page = specification.Page ?? new PageSpecification();

        return await query
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Job> ApplySorting(IQueryable<Job> query, JobSortField sortBy, SortDirection sortDirection)
    {
        return (sortBy, sortDirection) switch
        {
            (JobSortField.AssignmentStatus, SortDirection.Ascending) => query.OrderBy(x => x.AssignmentStatus).ThenBy(x => x.Id),
            (JobSortField.AssignmentStatus, SortDirection.Descending) => query.OrderByDescending(x => x.AssignmentStatus).ThenByDescending(x => x.Id),
            (JobSortField.CreatedAtUtc, SortDirection.Ascending) => query.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.Id),
            _ => query.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.Id),
        };
    }

    private static IQueryable<Job> ApplyTenantFilter(IQueryable<Job> query, Guid tenantId)
    {
        return query.Where(x => x.TenantId == tenantId);
    }
}
