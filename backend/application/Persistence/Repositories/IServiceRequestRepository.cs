using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IServiceRequestRepository : IRepository<ServiceRequest>
{
    Task<ServiceRequest?> GetByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default);
}
