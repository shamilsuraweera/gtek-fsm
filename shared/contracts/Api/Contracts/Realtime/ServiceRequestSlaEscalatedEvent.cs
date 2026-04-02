namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class ServiceRequestSlaEscalatedEvent
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string SlaDimension { get; set; } = string.Empty;

    public string PreviousSlaStatus { get; set; } = string.Empty;

    public string CurrentSlaStatus { get; set; } = string.Empty;

    public DateTime? DueAtUtc { get; set; }

    public DateTime TriggeredAtUtc { get; set; }

    public string? RowVersion { get; set; }
}
