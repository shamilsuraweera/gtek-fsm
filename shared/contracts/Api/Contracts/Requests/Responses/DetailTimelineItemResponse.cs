namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

/// <summary>
/// Response DTO for one timeline item in a detail view.
/// </summary>
public class DetailTimelineItemResponse
{
    public string? EventType { get; set; }

    public string? Message { get; set; }

    public DateTime OccurredAtUtc { get; set; }

    public string? ActorUserId { get; set; }
}