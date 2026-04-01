namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

public sealed class ServiceRequestStatusUpdatedEvent
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string PreviousStatus { get; set; } = string.Empty;

    public string CurrentStatus { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string? RowVersion { get; set; }
}