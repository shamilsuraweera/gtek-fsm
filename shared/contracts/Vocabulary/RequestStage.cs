namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Workflow stages for request lifecycle management.
/// Represents the progression of a request from creation through completion or cancellation.
/// </summary>
public enum RequestStage
{
    /// <summary>Request has been created but not yet assigned.</summary>
    New = 0,

    /// <summary>Request has been assigned to a worker.</summary>
    Assigned = 1,

    /// <summary>Worker has begun work on the request.</summary>
    InProgress = 2,

    /// <summary>Request is temporarily paused or waiting for external input.</summary>
    OnHold = 3,

    /// <summary>Request work has been finished and accepted.</summary>
    Completed = 4,

    /// <summary>Request was cancelled before completion.</summary>
    Cancelled = 5
}
