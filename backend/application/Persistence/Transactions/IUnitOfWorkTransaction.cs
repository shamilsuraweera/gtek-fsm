namespace GTEK.FSM.Backend.Application.Persistence.Transactions;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Guid? TransactionId { get; }

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
