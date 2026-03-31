using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Events;
using GTEK.FSM.Backend.Domain.Policies;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// ServiceRequest aggregate root.
/// Owned by one tenant and created by a customer user from the same tenant.
/// </summary>
public sealed class ServiceRequest
{
    private readonly List<IDomainEvent> domainEvents = new();

    public ServiceRequest(Guid id, Guid tenantId, Guid customerUserId, string title)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Request id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Request must belong to a tenant.");
        this.CustomerUserId = DomainGuards.RequiredId(customerUserId, nameof(customerUserId), "Customer user id cannot be empty.");
        this.Title = DomainGuards.RequiredText(title, nameof(title), "Request title is required.", 180);
        this.Status = ServiceRequestStatus.New;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid CustomerUserId { get; }

    public string Title { get; private set; }

    public ServiceRequestStatus Status { get; private set; }

    public Guid? ActiveJobId { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public bool IsDeleted { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => this.domainEvents;

    public void Rename(string title)
    {
        if (this.Status is ServiceRequestStatus.Completed or ServiceRequestStatus.Cancelled)
        {
            throw new InvalidOperationException("Cannot rename a request in terminal status.");
        }

        this.Title = DomainGuards.RequiredText(title, nameof(title), "Request title is required.", 180);
    }

    public void LinkJob(Guid jobId)
    {
        if (this.Status != ServiceRequestStatus.Assigned)
        {
            throw new InvalidOperationException("Job can be linked only when request status is Assigned.");
        }

        if (this.ActiveJobId.HasValue)
        {
            throw new InvalidOperationException("Request already has an active linked job.");
        }

        this.ActiveJobId = DomainGuards.RequiredId(jobId, nameof(jobId), "Job id cannot be empty.");
    }

    public void TransitionTo(ServiceRequestStatus nextStatus)
    {
        if (nextStatus == ServiceRequestStatus.Completed && !this.ActiveJobId.HasValue)
        {
            throw new InvalidOperationException("Request cannot be completed without an active job.");
        }

        if (!ServiceRequestStateTransitions.CanTransition(this.Status, nextStatus))
        {
            throw new InvalidOperationException($"Invalid request transition: {this.Status} -> {nextStatus}.");
        }

        var previousStatus = this.Status;
        this.Status = nextStatus;
        this.AddDomainEvent(new ServiceRequestStatusChangedDomainEvent(this.Id, this.TenantId, previousStatus, nextStatus));
    }

    public void UnlinkJob()
    {
        if (!this.ActiveJobId.HasValue)
        {
            throw new InvalidOperationException("Request does not have an active job to unlink.");
        }

        if (this.Status == ServiceRequestStatus.InProgress)
        {
            throw new InvalidOperationException("Cannot unlink job while request is in progress.");
        }

        this.ActiveJobId = null;
    }

    public void ClearDomainEvents()
    {
        this.domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        this.domainEvents.Add(domainEvent);
    }
}
