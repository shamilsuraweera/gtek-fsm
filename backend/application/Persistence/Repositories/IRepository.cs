namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

/// <summary>
/// Base write contract for aggregate repositories.
/// </summary>
/// <typeparam name="TAggregate">Aggregate root type.</typeparam>
public interface IRepository<TAggregate>
    where TAggregate : class
{
    Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
