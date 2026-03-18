using GTEK.FSM.Backend.Domain.Events;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Job aggregate root.
/// Belongs to one tenant, derives from one service request, and can be assigned to one worker.
/// </summary>
public sealed class Job
{
    private readonly List<IDomainEvent> domainEvents = new();

    public Job(Guid id, Guid tenantId, Guid serviceRequestId)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Job id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Job must belong to a tenant.");
        this.ServiceRequestId = DomainGuards.RequiredId(serviceRequestId, nameof(serviceRequestId), "Service request id cannot be empty.");
        this.AssignmentStatus = AssignmentStatus.Unassigned;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid ServiceRequestId { get; }

    public AssignmentStatus AssignmentStatus { get; private set; }

    public Guid? AssignedWorkerUserId { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => this.domainEvents;

    public void AssignWorker(Guid workerUserId)
    {
        workerUserId = DomainGuards.RequiredId(workerUserId, nameof(workerUserId), "Worker user id cannot be empty.");

        if (this.AssignedWorkerUserId.HasValue)
        {
            throw new InvalidOperationException("Cannot assign another worker while assignment already exists.");
        }

        if (!AssignmentStateTransitions.CanTransition(this.AssignmentStatus, AssignmentStatus.PendingAcceptance))
        {
            throw new InvalidOperationException($"Invalid assignment transition: {this.AssignmentStatus} -> {AssignmentStatus.PendingAcceptance}.");
        }

        this.AssignedWorkerUserId = workerUserId;
        var previous = this.AssignmentStatus;
        this.AssignmentStatus = AssignmentStatus.PendingAcceptance;
        this.AddDomainEvent(new JobAssignmentStatusChangedDomainEvent(this.Id, this.TenantId, previous, this.AssignmentStatus, this.AssignedWorkerUserId));
    }

    public void MarkAccepted()
    {
        EnsureWorkerAssigned();
        this.TransitionAssignment(AssignmentStatus.Accepted);
    }

    public void MarkRejected()
    {
        EnsureWorkerAssigned();
        this.TransitionAssignment(AssignmentStatus.Rejected);
    }

    public void MarkCompleted()
    {
        EnsureWorkerAssigned();
        this.TransitionAssignment(AssignmentStatus.Completed);
    }

    public void MarkCancelled()
    {
        this.TransitionAssignment(AssignmentStatus.Cancelled);
    }

    public void UnassignWorker()
    {
        if (!this.AssignedWorkerUserId.HasValue)
        {
            throw new InvalidOperationException("Job is already unassigned.");
        }

        if (this.AssignmentStatus == AssignmentStatus.Accepted)
        {
            throw new InvalidOperationException("Cannot unassign an accepted job. Cancel assignment first.");
        }

        if (this.AssignmentStatus == AssignmentStatus.Completed)
        {
            throw new InvalidOperationException("Cannot unassign a completed job.");
        }

        this.AssignedWorkerUserId = null;
        var previous = this.AssignmentStatus;
        this.AssignmentStatus = AssignmentStatus.Unassigned;
        this.AddDomainEvent(new JobAssignmentStatusChangedDomainEvent(this.Id, this.TenantId, previous, this.AssignmentStatus, this.AssignedWorkerUserId));
    }

    private void EnsureWorkerAssigned()
    {
        if (!this.AssignedWorkerUserId.HasValue)
        {
            throw new InvalidOperationException("Cannot transition assignment state without an assigned worker.");
        }
    }

    private void TransitionAssignment(AssignmentStatus nextStatus)
    {
        if (!AssignmentStateTransitions.CanTransition(this.AssignmentStatus, nextStatus))
        {
            throw new InvalidOperationException($"Invalid assignment transition: {this.AssignmentStatus} -> {nextStatus}.");
        }

        var previous = this.AssignmentStatus;
        this.AssignmentStatus = nextStatus;
        this.AddDomainEvent(new JobAssignmentStatusChangedDomainEvent(this.Id, this.TenantId, previous, this.AssignmentStatus, this.AssignedWorkerUserId));
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
