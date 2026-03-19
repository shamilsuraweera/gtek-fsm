using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByIdAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    Task<User?> GetByExternalIdentityAsync(Guid tenantId, string externalIdentity, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
