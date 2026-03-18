using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Job aggregate root.
/// Belongs to one tenant, derives from one service request, and can be assigned to one worker.
/// </summary>
public sealed class Job
{
    public Job(Guid id, Guid tenantId, Guid serviceRequestId)
    {
        this.Id = id != Guid.Empty ? id : throw new ArgumentException("Job id cannot be empty.", nameof(id));
        this.TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("Job must belong to a tenant.", nameof(tenantId));
        this.ServiceRequestId = serviceRequestId != Guid.Empty
            ? serviceRequestId
            : throw new ArgumentException("Service request id cannot be empty.", nameof(serviceRequestId));
        this.AssignmentStatus = AssignmentStatus.Unassigned;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid ServiceRequestId { get; }

    public AssignmentStatus AssignmentStatus { get; private set; }

    public Guid? AssignedWorkerUserId { get; private set; }

    public void AssignWorker(Guid workerUserId)
    {
        if (workerUserId == Guid.Empty)
        {
            throw new ArgumentException("Worker user id cannot be empty.", nameof(workerUserId));
        }

        if (!AssignmentStateTransitions.CanTransition(this.AssignmentStatus, AssignmentStatus.PendingAcceptance))
        {
            throw new InvalidOperationException($"Invalid assignment transition: {this.AssignmentStatus} -> {AssignmentStatus.PendingAcceptance}.");
        }

        this.AssignedWorkerUserId = workerUserId;
        this.AssignmentStatus = AssignmentStatus.PendingAcceptance;
    }

    public void MarkAccepted()
    {
        this.TransitionAssignment(AssignmentStatus.Accepted);
    }

    public void MarkRejected()
    {
        this.TransitionAssignment(AssignmentStatus.Rejected);
    }

    public void MarkCompleted()
    {
        this.TransitionAssignment(AssignmentStatus.Completed);
    }

    public void MarkCancelled()
    {
        this.TransitionAssignment(AssignmentStatus.Cancelled);
    }

    public void UnassignWorker()
    {
        if (this.AssignmentStatus == AssignmentStatus.Accepted)
        {
            throw new InvalidOperationException("Cannot unassign an accepted job. Cancel assignment first.");
        }

        this.AssignedWorkerUserId = null;
        this.AssignmentStatus = AssignmentStatus.Unassigned;
    }

    private void TransitionAssignment(AssignmentStatus nextStatus)
    {
        if (!AssignmentStateTransitions.CanTransition(this.AssignmentStatus, nextStatus))
        {
            throw new InvalidOperationException($"Invalid assignment transition: {this.AssignmentStatus} -> {nextStatus}.");
        }

        this.AssignmentStatus = nextStatus;
    }
}
