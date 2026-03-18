namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Requested or planned execution window in UTC.
/// </summary>
public sealed record SchedulingWindow
{
    public SchedulingWindow(DateTime startsOnUtc, DateTime endsOnUtc)
    {
        if (startsOnUtc.Kind != DateTimeKind.Utc || endsOnUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Scheduling window timestamps must be UTC.");
        }

        if (endsOnUtc <= startsOnUtc)
        {
            throw new ArgumentException("Scheduling window end must be after start.", nameof(endsOnUtc));
        }

        this.StartsOnUtc = startsOnUtc;
        this.EndsOnUtc = endsOnUtc;
    }

    public DateTime StartsOnUtc { get; }

    public DateTime EndsOnUtc { get; }

    public TimeSpan Duration => this.EndsOnUtc - this.StartsOnUtc;

    public bool Overlaps(SchedulingWindow other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return this.StartsOnUtc < other.EndsOnUtc && other.StartsOnUtc < this.EndsOnUtc;
    }
}
