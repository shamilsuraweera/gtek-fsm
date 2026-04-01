namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents one timeline entry in a request or job detail response.
/// </summary>
/// <param name="EventType">Machine-readable event type.</param>
/// <param name="Message">Human-readable timeline message.</param>
/// <param name="OccurredAtUtc">Event timestamp in UTC.</param>
/// <param name="ActorUserId">Optional actor user id when available.</param>
public sealed record QueriedTimelineItem(
    string EventType,
    string Message,
    DateTime OccurredAtUtc,
    Guid? ActorUserId);