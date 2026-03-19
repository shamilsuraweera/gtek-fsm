using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IJobRepository : IRepository<Job>
{
    Task<Job?> GetByIdAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> ListByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Job>> ListByWorkerAsync(Guid tenantId, Guid workerUserId, CancellationToken cancellationToken = default);
}
