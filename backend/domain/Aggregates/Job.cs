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
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid ServiceRequestId { get; }

    public Guid? AssignedWorkerUserId { get; private set; }

    public void AssignWorker(Guid workerUserId)
    {
        this.AssignedWorkerUserId = workerUserId != Guid.Empty
            ? workerUserId
            : throw new ArgumentException("Worker user id cannot be empty.", nameof(workerUserId));
    }

    public void UnassignWorker()
    {
        this.AssignedWorkerUserId = null;
    }
}
