namespace GTEK.FSM.MobileApp.Services.Diagnostics;

using System.Diagnostics;
using GTEK.FSM.MobileApp.State;

public interface IMobileDiagnosticsLogger
{
    void Info(string category, string message);
    void Warn(string category, string message);
    void Error(string category, string message);
}

public sealed class MobileDiagnosticsLogger : IMobileDiagnosticsLogger
{
    private readonly MobileDiagnosticsState _state;

    public MobileDiagnosticsLogger(MobileDiagnosticsState state)
    {
        _state = state;
    }

    public void Info(string category, string message)
    {
        Write(MobileDiagnosticLevel.Info, category, message);
    }

    public void Warn(string category, string message)
    {
        Write(MobileDiagnosticLevel.Warning, category, message);
    }

    public void Error(string category, string message)
    {
        Write(MobileDiagnosticLevel.Error, category, message);
    }

    private void Write(MobileDiagnosticLevel level, string category, string message)
    {
        var diagnosticEvent = new MobileDiagnosticEvent(
            TimestampUtc: DateTimeOffset.UtcNow,
            Level: level,
            Category: category,
            Message: message);

        _state.Add(diagnosticEvent);
        Debug.WriteLine($"[{diagnosticEvent.TimestampUtc:O}] [{level}] [{category}] {message}");
    }
}
