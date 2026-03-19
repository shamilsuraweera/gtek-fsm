using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;
using GTEK.FSM.Backend.Infrastructure.Tests.TestUtils;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Repositories;

public class TenantSafetyQueryPathTests
{
    [Fact]
    public async Task UserRepository_QueryPaths_DoNotLeakAcrossTenants()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new UserRepository(dbContext);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var userA = new User(Guid.NewGuid(), tenantA, "ext-a", "Alice A");
        var userB = new User(Guid.NewGuid(), tenantB, "ext-b", "Bob B");

        await repository.AddAsync(userA);
        await repository.AddAsync(userB);
        await dbContext.SaveChangesAsync();

        var byId = await repository.GetByIdAsync(tenantA, userB.Id);
        var byExternalIdentity = await repository.GetByExternalIdentityAsync(tenantA, userB.ExternalIdentity);
        var listByTenant = await repository.ListByTenantAsync(tenantA);
        var queryBySpec = await repository.QueryAsync(new UserQuerySpecification(
            TenantId: tenantA,
            SearchText: "B",
            Page: new PageSpecification(1, 50),
            SortBy: UserSortField.DisplayName,
            SortDirection: SortDirection.Ascending));

        Assert.Null(byId);
        Assert.Null(byExternalIdentity);
        Assert.Single(listByTenant);
        Assert.Equal(userA.Id, listByTenant[0].Id);
        Assert.Empty(queryBySpec);
    }

    [Fact]
    public async Task ServiceRequestRepository_QueryPaths_DoNotLeakAcrossTenants()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new ServiceRequestRepository(dbContext);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        var requestA = new ServiceRequest(Guid.NewGuid(), tenantA, customerA, "A request");
        requestA.TransitionTo(ServiceRequestStatus.Assigned);

        var requestB = new ServiceRequest(Guid.NewGuid(), tenantB, customerB, "B request");
        requestB.TransitionTo(ServiceRequestStatus.Assigned);

        await repository.AddAsync(requestA);
        await repository.AddAsync(requestB);
        await dbContext.SaveChangesAsync();

        var byId = await repository.GetByIdAsync(tenantA, requestB.Id);
        var listByTenant = await repository.ListByTenantAsync(tenantA);
        var listByCustomer = await repository.ListByCustomerAsync(tenantA, customerB);
        var queryBySpec = await repository.QueryAsync(new ServiceRequestQuerySpecification(
            TenantId: tenantA,
            CustomerUserId: customerB,
            Status: ServiceRequestStatus.Assigned,
            SearchText: "B",
            Page: new PageSpecification(1, 50),
            SortBy: ServiceRequestSortField.Title,
            SortDirection: SortDirection.Ascending));

        Assert.Null(byId);
        Assert.Single(listByTenant);
        Assert.Equal(requestA.Id, listByTenant[0].Id);
        Assert.Empty(listByCustomer);
        Assert.Empty(queryBySpec);
    }

    [Fact]
    public async Task JobRepository_QueryPaths_DoNotLeakAcrossTenants()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new JobRepository(dbContext);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var requestA = Guid.NewGuid();
        var requestB = Guid.NewGuid();
        var workerA = Guid.NewGuid();
        var workerB = Guid.NewGuid();

        var jobA = new Job(Guid.NewGuid(), tenantA, requestA);
        jobA.AssignWorker(workerA);

        var jobB = new Job(Guid.NewGuid(), tenantB, requestB);
        jobB.AssignWorker(workerB);

        await repository.AddAsync(jobA);
        await repository.AddAsync(jobB);
        await dbContext.SaveChangesAsync();

        var byId = await repository.GetByIdAsync(tenantA, jobB.Id);
        var listByRequest = await repository.ListByServiceRequestAsync(tenantA, requestB);
        var listByWorker = await repository.ListByWorkerAsync(tenantA, workerB);
        var queryBySpec = await repository.QueryAsync(new JobQuerySpecification(
            TenantId: tenantA,
            ServiceRequestId: requestB,
            AssignedWorkerUserId: workerB,
            AssignmentStatus: AssignmentStatus.PendingAcceptance,
            Page: new PageSpecification(1, 50),
            SortBy: JobSortField.AssignmentStatus,
            SortDirection: SortDirection.Descending));

        Assert.Null(byId);
        Assert.Empty(listByRequest);
        Assert.Empty(listByWorker);
        Assert.Empty(queryBySpec);
    }

    [Fact]
    public async Task SubscriptionRepository_QueryPaths_DoNotLeakAcrossTenants()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new SubscriptionRepository(dbContext);

        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        var subscriptionA = new Subscription(Guid.NewGuid(), tenantA, "PRO", DateTime.UtcNow.AddDays(-10));
        var subscriptionB = new Subscription(Guid.NewGuid(), tenantB, "ENTERPRISE", DateTime.UtcNow.AddDays(-5));

        await repository.AddAsync(subscriptionA);
        await repository.AddAsync(subscriptionB);
        await dbContext.SaveChangesAsync();

        var byId = await repository.GetByIdAsync(tenantA, subscriptionB.Id);
        var active = await repository.GetActiveByTenantAsync(tenantA);
        var listByTenant = await repository.ListByTenantAsync(tenantA);
        var queryBySpec = await repository.QueryAsync(new SubscriptionQuerySpecification(
            TenantId: tenantA,
            ActiveOnly: true,
            PlanCode: "ENTERPRISE",
            Page: new PageSpecification(1, 50),
            SortBy: SubscriptionSortField.PlanCode,
            SortDirection: SortDirection.Ascending));

        Assert.Null(byId);
        Assert.NotNull(active);
        Assert.Equal(subscriptionA.Id, active!.Id);
        Assert.Single(listByTenant);
        Assert.Equal(subscriptionA.Id, listByTenant[0].Id);
        Assert.Empty(queryBySpec);
    }
}
