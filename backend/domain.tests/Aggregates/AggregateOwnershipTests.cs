using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Events;
using GTEK.FSM.Backend.Domain.Enums;
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
    public void ServiceRequest_ValidLifecycleTransition_Succeeds()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leaking pipe");

        request.TransitionTo(ServiceRequestStatus.Assigned);
        request.TransitionTo(ServiceRequestStatus.InProgress);

        Assert.Equal(ServiceRequestStatus.InProgress, request.Status);
    }

    [Fact]
    public void ServiceRequest_InvalidLifecycleTransition_Throws()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leaking pipe");

        Assert.Throws<InvalidOperationException>(() => request.TransitionTo(ServiceRequestStatus.Completed));
    }

    [Fact]
    public void ServiceRequest_LinkJob_WhenNotAssigned_Throws()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leaking pipe");

        Assert.Throws<InvalidOperationException>(() => request.LinkJob(Guid.NewGuid()));
    }

    [Fact]
    public void ServiceRequest_CompleteWithoutJob_Throws()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leaking pipe");
        request.TransitionTo(ServiceRequestStatus.Assigned);

        Assert.Throws<InvalidOperationException>(() => request.TransitionTo(ServiceRequestStatus.Completed));
    }

    [Fact]
    public void JobConstructor_WithoutServiceRequest_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.Empty));
    }

    [Fact]
    public void Job_ValidAssignmentLifecycleTransition_Succeeds()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        job.AssignWorker(Guid.NewGuid());
        job.MarkAccepted();
        job.MarkCompleted();

        Assert.Equal(AssignmentStatus.Completed, job.AssignmentStatus);
    }

    [Fact]
    public void Job_InvalidAssignmentLifecycleTransition_Throws()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => job.MarkCompleted());
    }

    [Fact]
    public void Job_AssignWorkerTwice_Throws()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        job.AssignWorker(Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => job.AssignWorker(Guid.NewGuid()));
    }

    [Fact]
    public void Job_UnassignWithoutAssignedWorker_Throws()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.Throws<InvalidOperationException>(() => job.UnassignWorker());
    }

    [Fact]
    public void SubscriptionConstructor_EndBeforeStart_Throws()
    {
        var start = DateTime.UtcNow;
        var end = start.AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            new Subscription(Guid.NewGuid(), Guid.NewGuid(), "BASIC", start, end));
    }

    [Fact]
    public void Subscription_ChangePlanAfterEnd_Throws()
    {
        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), "BASIC", DateTime.UtcNow);
        subscription.End(DateTime.UtcNow.AddDays(1));

        Assert.Throws<InvalidOperationException>(() => subscription.ChangePlan("PREMIUM"));
    }

    [Fact]
    public void ServiceRequest_Transition_CapturesDomainEvent()
    {
        var request = new ServiceRequest(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Leaking pipe");

        request.TransitionTo(ServiceRequestStatus.Assigned);

        var domainEvent = Assert.Single(request.DomainEvents);
        var typed = Assert.IsType<ServiceRequestStatusChangedDomainEvent>(domainEvent);
        Assert.Equal(ServiceRequestStatus.New, typed.PreviousStatus);
        Assert.Equal(ServiceRequestStatus.Assigned, typed.CurrentStatus);
    }

    [Fact]
    public void Job_AssignmentStatusChange_CapturesDomainEvent()
    {
        var job = new Job(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        job.AssignWorker(Guid.NewGuid());

        var domainEvent = Assert.Single(job.DomainEvents);
        var typed = Assert.IsType<JobAssignmentStatusChangedDomainEvent>(domainEvent);
        Assert.Equal(AssignmentStatus.Unassigned, typed.PreviousStatus);
        Assert.Equal(AssignmentStatus.PendingAcceptance, typed.CurrentStatus);
    }

    [Fact]
    public void Subscription_ChangePlan_CapturesDomainEvent()
    {
        var subscription = new Subscription(Guid.NewGuid(), Guid.NewGuid(), "BASIC", DateTime.UtcNow);

        subscription.ChangePlan("PREMIUM");

        var domainEvent = Assert.Single(subscription.DomainEvents);
        var typed = Assert.IsType<SubscriptionPlanChangedDomainEvent>(domainEvent);
        Assert.Equal("BASIC", typed.PreviousPlanCode);
        Assert.Equal("PREMIUM", typed.CurrentPlanCode);
    }

    [Fact]
    public void Tenant_AttachSubscription_CapturesAndClearsDomainEvent()
    {
        var tenant = new Tenant(Guid.NewGuid(), "acme", "Acme Corp");

        tenant.AttachSubscription(Guid.NewGuid());
        Assert.Single(tenant.DomainEvents);

        tenant.ClearDomainEvents();
        Assert.Empty(tenant.DomainEvents);
    }
}
