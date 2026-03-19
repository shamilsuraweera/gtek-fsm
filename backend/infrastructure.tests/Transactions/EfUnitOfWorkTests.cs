using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;
using GTEK.FSM.Backend.Infrastructure.Persistence.Transactions;
using GTEK.FSM.Backend.Infrastructure.Tests.TestUtils;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Transactions;

public class EfUnitOfWorkTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenEntityAdded_PersistsData()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        ITenantRepository tenantRepository = new TenantRepository(dbContext);
        var unitOfWork = new EfUnitOfWork(dbContext);

        await tenantRepository.AddAsync(new Tenant(Guid.NewGuid(), "acme", "Acme Corp"));
        var affected = await unitOfWork.SaveChangesAsync();

        Assert.Equal(1, affected);
        Assert.Single(dbContext.Tenants);
    }
}
