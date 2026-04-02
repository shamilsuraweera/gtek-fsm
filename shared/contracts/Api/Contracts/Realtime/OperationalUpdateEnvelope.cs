namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class OperationalUpdateEnvelope
{
    public string EventType { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public ServiceRequestStatusUpdatedEvent? ServiceRequestStatusUpdated { get; set; }

    public JobAssignmentUpdatedEvent? JobAssignmentUpdated { get; set; }

    public ServiceRequestSlaEscalatedEvent? ServiceRequestSlaEscalated { get; set; }
}