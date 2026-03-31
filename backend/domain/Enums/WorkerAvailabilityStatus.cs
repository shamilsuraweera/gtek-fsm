namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Worker availability states used during assignment and scheduling.
/// </summary>
public enum WorkerAvailabilityStatus
{
    /// <summary>The worker is offline and not reachable for assignments.</summary>
    Offline = 0,

    /// <summary>The worker is available for new assignments.</summary>
    Available = 1,

    /// <summary>The worker is currently engaged in active work.</summary>
    Busy = 2,

    /// <summary>The worker is temporarily on break.</summary>
    OnBreak = 3,

    /// <summary>The worker is unavailable for assignment intake.</summary>
    Unavailable = 4
}
