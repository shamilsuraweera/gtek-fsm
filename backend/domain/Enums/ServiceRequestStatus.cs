namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Lifecycle states for a service request.
/// </summary>
public enum ServiceRequestStatus
{
    /// <summary>The request was created and awaits handling.</summary>
    New = 0,

    /// <summary>The request has an assignment in place.</summary>
    Assigned = 1,

    /// <summary>Work on the request is actively in progress.</summary>
    InProgress = 2,

    /// <summary>The request is temporarily paused.</summary>
    OnHold = 3,

    /// <summary>The request work has finished successfully.</summary>
    Completed = 4,

    /// <summary>The request was cancelled before completion.</summary>
    Cancelled = 5
}
