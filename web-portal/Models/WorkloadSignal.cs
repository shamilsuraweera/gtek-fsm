namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Workload prioritization signal combining request priority and age/urgency.
/// </summary>
public record WorkloadSignal(
    UrgencyLevel UrgencyLevel,
    int AgeMinutes,
    bool IsEscalated,
    bool IsSLABreach,
    string ContextHint);
