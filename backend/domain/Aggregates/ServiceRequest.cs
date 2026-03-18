using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// ServiceRequest aggregate root.
/// Owned by one tenant and created by a customer user from the same tenant.
/// </summary>
public sealed class ServiceRequest
{
    public ServiceRequest(Guid id, Guid tenantId, Guid customerUserId, string title)
    {
        this.Id = id != Guid.Empty ? id : throw new ArgumentException("Request id cannot be empty.", nameof(id));
        this.TenantId = tenantId != Guid.Empty ? tenantId : throw new ArgumentException("Request must belong to a tenant.", nameof(tenantId));
        this.CustomerUserId = customerUserId != Guid.Empty
            ? customerUserId
            : throw new ArgumentException("Customer user id cannot be empty.", nameof(customerUserId));
        this.Title = !string.IsNullOrWhiteSpace(title) ? title.Trim() : throw new ArgumentException("Request title is required.", nameof(title));
        this.Status = ServiceRequestStatus.New;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid CustomerUserId { get; }

    public string Title { get; private set; }

    public ServiceRequestStatus Status { get; private set; }

    public Guid? ActiveJobId { get; private set; }

    public void Rename(string title)
    {
        this.Title = !string.IsNullOrWhiteSpace(title) ? title.Trim() : throw new ArgumentException("Request title is required.", nameof(title));
    }

    public void LinkJob(Guid jobId)
    {
        this.ActiveJobId = jobId != Guid.Empty ? jobId : throw new ArgumentException("Job id cannot be empty.", nameof(jobId));
    }

    public void TransitionTo(ServiceRequestStatus nextStatus)
    {
        if (!ServiceRequestStateTransitions.CanTransition(this.Status, nextStatus))
        {
            throw new InvalidOperationException($"Invalid request transition: {this.Status} -> {nextStatus}.");
        }

        this.Status = nextStatus;
    }

    public void UnlinkJob()
    {
        this.ActiveJobId = null;
    }
}
