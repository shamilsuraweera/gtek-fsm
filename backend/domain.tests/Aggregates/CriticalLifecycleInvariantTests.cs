using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using Xunit;

namespace GTEK.FSM.Backend.Domain.Tests.Aggregates;

public class CriticalLifecycleInvariantTests
{
    [Fact]
    public void ServiceRequest_RenameInTerminalStatus_Throws()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pipe leak");
        request.TransitionTo(ServiceRequestStatus.Assigned);
        request.LinkJob(Guid.NewGuid());
        request.TransitionTo(ServiceRequestStatus.InProgress);
        request.TransitionTo(ServiceRequestStatus.Completed);

        Assert.Throws<InvalidOperationException>(() => request.Rename("New title"));
    }

    [Fact]
    public void ServiceRequest_UnlinkJobDuringInProgress_Throws()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Pipe leak");
        request.TransitionTo(ServiceRequestStatus.Assigned);
        request.LinkJob(Guid.NewGuid());
        request.TransitionTo(ServiceRequestStatus.InProgress);

        Assert.Throws<InvalidOperationException>(() => request.UnlinkJob());
    }

    [Fact]
    public void Job_UnassignAfterAccepted_Throws()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        job.AssignWorker(Guid.NewGuid());
        job.MarkAccepted();

        Assert.Throws<InvalidOperationException>(() => job.UnassignWorker());
    }

    [Fact]
    public void Job_InvalidAcceptedToPendingAcceptance_Throws()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        job.AssignWorker(Guid.NewGuid());
        job.MarkAccepted();

        Assert.Throws<InvalidOperationException>(() => job.AssignWorker(Guid.NewGuid()));
    }

    [Fact]
    public void Subscription_EndTwice_Throws()
    {
        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), "PRO", DateTime.UtcNow);
        subscription.End(DateTime.UtcNow.AddDays(30));

        Assert.Throws<InvalidOperationException>(() => subscription.End(DateTime.UtcNow.AddDays(60)));
    }
}
