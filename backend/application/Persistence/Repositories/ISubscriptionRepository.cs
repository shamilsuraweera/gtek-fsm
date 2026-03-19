using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Application.Persistence.Specifications;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface ISubscriptionRepository : IRepository<Subscription>
{
    Task<Subscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default);

    Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subscription>> QueryAsync(SubscriptionQuerySpecification specification, CancellationToken cancellationToken = default);
}
