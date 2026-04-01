using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Transactions;

internal sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly GtekFsmDbContext dbContext;

    public EfUnitOfWork(GtekFsmDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (this.dbContext.Database.CurrentTransaction is not null)
        {
            throw new InvalidOperationException("A database transaction is already active for this unit of work.");
        }

        var transaction = await this.dbContext.Database.BeginTransactionAsync(cancellationToken);
        return new EfUnitOfWorkTransaction(transaction);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await this.dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrencyConflictException("The requested update conflicted with a newer version of the data.", exception);
        }
    }
}
