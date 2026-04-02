namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// SLA state classification for a tracked SLA window.
/// </summary>
public enum SlaState : byte
{
    NotApplicable = 0,
    OnTrack = 1,
    AtRisk = 2,
    Breached = 3,
}
