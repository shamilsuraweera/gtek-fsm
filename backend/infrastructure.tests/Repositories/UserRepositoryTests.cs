using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;
using GTEK.FSM.Backend.Infrastructure.Tests.TestUtils;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Repositories;

public class UserRepositoryTests
{
    [Fact]
    public async Task QueryAsync_WhenFilteringByTenant_ReturnsOnlyTenantUsers()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new UserRepository(dbContext);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await repository.AddAsync(new User(Guid.NewGuid(), tenantA, "a-1", "Alice"));
        await repository.AddAsync(new User(Guid.NewGuid(), tenantA, "a-2", "Aaron"));
        await repository.AddAsync(new User(Guid.NewGuid(), tenantB, "b-1", "Bob"));
        await dbContext.SaveChangesAsync();

        var result = await repository.QueryAsync(new UserQuerySpecification(
            TenantId: tenantA,
            Page: new PageSpecification(1, 10),
            SortBy: UserSortField.DisplayName,
            SortDirection: SortDirection.Ascending));

        Assert.Equal(2, result.Count);
        Assert.All(result, user => Assert.Equal(tenantA, user.TenantId));
        Assert.Equal(new[] { "Aaron", "Alice" }, result.Select(x => x.DisplayName).ToArray());
    }
}
