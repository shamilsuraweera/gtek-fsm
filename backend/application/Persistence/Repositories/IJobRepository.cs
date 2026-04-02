using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Application.Persistence.Specifications;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<Job?> GetForUpdateAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> ListByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> QueryAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(JobQuerySpecification specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, int>> GetActiveJobCountsByWorkerAsync(
        Guid tenantId,
        IReadOnlyList<Guid> workerIds,
        CancellationToken cancellationToken = default);
}
