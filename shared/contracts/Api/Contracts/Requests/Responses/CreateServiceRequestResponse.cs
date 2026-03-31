namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

/// <summary>
/// Response payload returned when a service request is created.
/// </summary>
public sealed class CreateServiceRequestResponse
{
    public string RequestId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string CustomerUserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
