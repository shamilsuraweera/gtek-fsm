using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IWorkerProfileRepository : IRepository<WorkerProfile>
{
    Task<WorkerProfile?> GetByIdAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default);

    Task<WorkerProfile?> GetForUpdateAsync(Guid tenantId, Guid workerId, CancellationToken cancellationToken = default);

    Task<WorkerProfile?> GetByCodeAsync(Guid tenantId, string workerCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkerProfile>> QueryAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(WorkerProfileQuerySpecification specification, CancellationToken cancellationToken = default);
}
