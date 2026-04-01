using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface ICategoryRepository : IRepository<ServiceCategory>
{
    Task<ServiceCategory?> GetByIdAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default);

    Task<ServiceCategory?> GetForUpdateAsync(Guid tenantId, Guid categoryId, CancellationToken cancellationToken = default);

    Task<ServiceCategory?> GetByCodeAsync(Guid tenantId, string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCategory>> ListByTenantAsync(Guid tenantId, bool includeDisabled, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceCategory>> ListActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}