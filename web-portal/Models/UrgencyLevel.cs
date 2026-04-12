namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Urgency indicator based on request age, priority, and SLA context.
/// </summary>
public enum UrgencyLevel
{
    /// <summary>Normal urgency; no immediate action required.</summary>
    Normal = 0,

    /// <summary>Moderate urgency; action recommended soon.</summary>
    Moderate = 1,

    /// <summary>High urgency; action required within SLA window.</summary>
    High = 2,

    /// <summary>Critical urgency; SLA approaching or breached.</summary>
    Critical = 3,
}
