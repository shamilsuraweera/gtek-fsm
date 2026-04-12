namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Represents the result of a triage action.
/// </summary>
public record TriageActionResult(
    bool Success,
    string? Message = null,
    Exception? Error = null);
