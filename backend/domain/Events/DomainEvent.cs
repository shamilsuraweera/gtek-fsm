namespace GTEK.FSM.Backend.Domain.Events;

/// <summary>
/// Base record for domain events.
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    protected DomainEvent(string eventName)
    {
        this.EventName = eventName;
        this.OccurredOnUtc = DateTime.UtcNow;
    }

    public DateTime OccurredOnUtc { get; }

    public string EventName { get; }
}
