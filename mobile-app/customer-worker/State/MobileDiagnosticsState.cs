namespace GTEK.FSM.MobileApp.State;

public enum MobileDiagnosticLevel
{
    Info,
    Warning,
    Error,
}

public sealed record MobileDiagnosticEvent(
    DateTimeOffset TimestampUtc,
    MobileDiagnosticLevel Level,
    string Category,
    string Message);

public sealed class MobileDiagnosticsState
{
    private const int MaxEvents = 200;
    private readonly Queue<MobileDiagnosticEvent> _events = new();

    public IReadOnlyCollection<MobileDiagnosticEvent> Events => _events.ToArray();

    public void Add(MobileDiagnosticEvent diagnosticEvent)
    {
        _events.Enqueue(diagnosticEvent);

        while (_events.Count > MaxEvents)
        {
            _events.Dequeue();
        }
    }

    public void Clear()
    {
        _events.Clear();
    }
}
