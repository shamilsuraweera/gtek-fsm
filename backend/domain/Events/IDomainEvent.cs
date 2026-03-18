namespace GTEK.FSM.Backend.Domain.Events;

/// <summary>
/// Minimal domain event contract.
/// Events are captured in-memory on aggregates and dispatched later by application/infrastructure layers.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOnUtc { get; }

    string EventName { get; }
}
