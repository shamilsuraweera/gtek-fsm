namespace GTEK.FSM.WebPortal.Models;

/// <summary>
/// Represents available triage and action operations on requests/assignments.
/// </summary>
public enum TriageAction
{
    /// <summary>Assign the request to a worker.</summary>
    Assign = 0,

    /// <summary>Escalate the request to a higher priority or support tier.</summary>
    Escalate = 1,

    /// <summary>Mark the request as resolved or completed.</summary>
    Complete = 2,

    /// <summary>Hold or pause the request for later action.</summary>
    Hold = 3,

    /// <summary>Return the request to an earlier stage.</summary>
    Reopen = 4,

    /// <summary>Reject or cancel the request.</summary>
    Reject = 5,

    /// <summary>Reassign the request to a different worker.</summary>
    Reassign = 6,

    /// <summary>Request additional information from the customer.</summary>
    RequestInfo = 7,

    /// <summary>View details and history of the request.</summary>
    ViewDetails = 8,

    /// <summary>Add a note or comment to the request.</summary>
    AddNote = 9,
}
