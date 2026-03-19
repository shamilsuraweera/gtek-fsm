using GTEK.FSM.Backend.Application.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;

internal abstract class EfRepository<TAggregate> : IRepository<TAggregate>
    where TAggregate : class
{
    private readonly GtekFsmDbContext dbContext;

    protected EfRepository(GtekFsmDbContext dbContext)
    {
        this.dbContext = dbContext;
    }

    protected IQueryable<TAggregate> Queryable()
    {
        return this.dbContext.Set<TAggregate>();
    }

    public Task AddAsync(TAggregate aggregate, CancellationToken cancellationToken = default)
    {
        return this.dbContext.Set<TAggregate>().AddAsync(aggregate, cancellationToken).AsTask();
    }

    public void Update(TAggregate aggregate)
    {
        this.dbContext.Set<TAggregate>().Update(aggregate);
    }

    public void Remove(TAggregate aggregate)
    {
        this.dbContext.Set<TAggregate>().Remove(aggregate);
    }
}
