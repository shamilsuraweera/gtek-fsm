using GTEK.FSM.Backend.Domain.Aggregates;
using Xunit;

namespace GTEK.FSM.Backend.Domain.Tests.Aggregates;

public class AggregateOwnershipTests
{
    [Fact]
    public void TenantConstructor_WithValidData_CreatesAggregate()
    {
        var tenant = new Tenant(Guid.NewGuid(), "acme", "Acme Corp");

        Assert.NotEqual(Guid.Empty, tenant.Id);
        Assert.Equal("acme", tenant.Code);
        Assert.Equal("Acme Corp", tenant.Name);
    }

    [Fact]
    public void UserConstructor_WithoutTenant_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new User(Guid.NewGuid(), Guid.Empty, "ext-001", "John"));
    }

    [Fact]
    public void ServiceRequestConstructor_WithoutTenant_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new ServiceRequest(Guid.NewGuid(), Guid.Empty, Guid.NewGuid(), "Leaking pipe"));
    }

    [Fact]
    public void JobConstructor_WithoutServiceRequest_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void SubscriptionConstructor_EndBeforeStart_Throws()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            new Subscription(Guid.NewGuid(), Guid.NewGuid(), "BASIC", start, end));
    }
}
