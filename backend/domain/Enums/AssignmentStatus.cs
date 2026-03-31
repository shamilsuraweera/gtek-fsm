namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Assignment lifecycle for job-worker matching.
/// </summary>
public enum AssignmentStatus
{
    /// <summary>No worker is assigned yet.</summary>
    Unassigned = 0,

    /// <summary>A worker has been assigned and is pending acceptance.</summary>
    PendingAcceptance = 1,

    /// <summary>The assigned worker accepted the assignment.</summary>
    Accepted = 2,

    /// <summary>The assigned worker rejected the assignment.</summary>
    Rejected = 3,

    /// <summary>The job was completed by the assigned worker.</summary>
    Completed = 4,

    /// <summary>The assignment was cancelled.</summary>
    Cancelled = 5
}
