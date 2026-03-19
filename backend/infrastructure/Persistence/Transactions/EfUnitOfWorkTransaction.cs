using GTEK.FSM.Backend.Application.Persistence.Transactions;
using Microsoft.EntityFrameworkCore.Storage;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Transactions;

internal sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction dbTransaction;
    private bool completed;

    public EfUnitOfWorkTransaction(IDbContextTransaction dbTransaction)
    {
        this.dbTransaction = dbTransaction;
    }

    public Guid? TransactionId => this.dbTransaction.TransactionId;

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (this.completed)
        {
            return;
        }

        await this.dbTransaction.CommitAsync(cancellationToken);
        this.completed = true;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (this.completed)
        {
            return;
        }

        await this.dbTransaction.RollbackAsync(cancellationToken);
        this.completed = true;
    }

    public async ValueTask DisposeAsync()
    {
        if (!this.completed)
        {
            await this.dbTransaction.RollbackAsync();
            this.completed = true;
        }

        await this.dbTransaction.DisposeAsync();
    }
}
