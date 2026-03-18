namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Worker availability states used during assignment and scheduling.
/// </summary>
public enum WorkerAvailabilityStatus
{
    Offline = 0,
    Available = 1,
    Busy = 2,
    OnBreak = 3,
    Unavailable = 4
}
