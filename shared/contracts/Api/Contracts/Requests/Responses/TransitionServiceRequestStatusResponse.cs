namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

/// <summary>
/// Response payload returned after a successful service request status transition.
/// </summary>
public sealed class TransitionServiceRequestStatusResponse
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string PreviousStatus { get; set; } = string.Empty;

    public string CurrentStatus { get; set; } = string.Empty;

    public DateTime UpdatedAtUtc { get; set; }

    public string? RowVersion { get; set; }
}
