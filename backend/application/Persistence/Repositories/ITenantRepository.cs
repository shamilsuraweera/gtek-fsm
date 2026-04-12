using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface ITenantRepository : IRepository<Tenant>
{
    Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default);

    Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<Tenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default);

    Task<bool> ExistsByCodeAsync(string tenantCode, CancellationToken cancellationToken = default);
}
