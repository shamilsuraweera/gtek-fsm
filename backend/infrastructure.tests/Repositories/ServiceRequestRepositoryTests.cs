using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;
using GTEK.FSM.Backend.Infrastructure.Tests.TestUtils;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Repositories;

public class ServiceRequestRepositoryTests
{
    [Fact]
    public async Task QueryAsync_WhenFilteringByStatusAndCustomer_ReturnsExpectedRows()
    {
        await using var dbContext = TestDbContextFactory.CreateInMemory();
        var repository = new ServiceRequestRepository(dbContext);

        var tenantId = Guid.NewGuid();
        var customerA = Guid.NewGuid();
        var customerB = Guid.NewGuid();

        var request1 = new ServiceRequest(Guid.NewGuid(), tenantId, customerA, "Fix lights");
        request1.TransitionTo(ServiceRequestStatus.Assigned);

        var request2 = new ServiceRequest(Guid.NewGuid(), tenantId, customerA, "Fix sink");
        request2.TransitionTo(ServiceRequestStatus.Assigned);

        var request3 = new ServiceRequest(Guid.NewGuid(), tenantId, customerB, "Fix door");
        request3.TransitionTo(ServiceRequestStatus.Assigned);

        await repository.AddAsync(request1);
        await repository.AddAsync(request2);
        await repository.AddAsync(request3);
        await dbContext.SaveChangesAsync();

        var result = await repository.QueryAsync(new ServiceRequestQuerySpecification(
            TenantId: tenantId,
            CustomerUserId: customerA,
            Status: ServiceRequestStatus.Assigned,
            Page: new PageSpecification(1, 1),
            SortBy: ServiceRequestSortField.Title,
            SortDirection: SortDirection.Ascending));

        Assert.Single(result);
        Assert.Equal("Fix lights", result[0].Title);
    }
}
