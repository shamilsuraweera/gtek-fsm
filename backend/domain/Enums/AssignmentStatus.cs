namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Assignment lifecycle for job-worker matching.
/// </summary>
public enum AssignmentStatus
{
    Unassigned = 0,
    PendingAcceptance = 1,
    Accepted = 2,
    Rejected = 3,
    Completed = 4,
    Cancelled = 5
}
