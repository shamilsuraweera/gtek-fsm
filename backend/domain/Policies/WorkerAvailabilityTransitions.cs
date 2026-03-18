using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Policies;

/// <summary>
/// Allowed transition policy for worker availability lifecycle.
/// </summary>
public static class WorkerAvailabilityTransitions
{
    public static bool CanTransition(WorkerAvailabilityStatus from, WorkerAvailabilityStatus to)
    {
        return (from, to) switch
        {
            (WorkerAvailabilityStatus.Offline, WorkerAvailabilityStatus.Available) => true,
            (WorkerAvailabilityStatus.Offline, WorkerAvailabilityStatus.Unavailable) => true,
            (WorkerAvailabilityStatus.Available, WorkerAvailabilityStatus.Busy) => true,
            (WorkerAvailabilityStatus.Available, WorkerAvailabilityStatus.OnBreak) => true,
            (WorkerAvailabilityStatus.Available, WorkerAvailabilityStatus.Unavailable) => true,
            (WorkerAvailabilityStatus.Busy, WorkerAvailabilityStatus.Available) => true,
            (WorkerAvailabilityStatus.Busy, WorkerAvailabilityStatus.OnBreak) => true,
            (WorkerAvailabilityStatus.Busy, WorkerAvailabilityStatus.Unavailable) => true,
            (WorkerAvailabilityStatus.OnBreak, WorkerAvailabilityStatus.Available) => true,
            (WorkerAvailabilityStatus.OnBreak, WorkerAvailabilityStatus.Unavailable) => true,
            (WorkerAvailabilityStatus.Unavailable, WorkerAvailabilityStatus.Offline) => true,
            (WorkerAvailabilityStatus.Unavailable, WorkerAvailabilityStatus.Available) => true,
            (WorkerAvailabilityStatus.Available, WorkerAvailabilityStatus.Available) => true,
            (WorkerAvailabilityStatus.Busy, WorkerAvailabilityStatus.Busy) => true,
            (WorkerAvailabilityStatus.OnBreak, WorkerAvailabilityStatus.OnBreak) => true,
            (WorkerAvailabilityStatus.Unavailable, WorkerAvailabilityStatus.Unavailable) => true,
            (WorkerAvailabilityStatus.Offline, WorkerAvailabilityStatus.Offline) => true,
            _ => false
        };
    }
}
