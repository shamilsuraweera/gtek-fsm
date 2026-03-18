namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Lifecycle states for a service request.
/// </summary>
public enum ServiceRequestStatus
{
    New = 0,
    Assigned = 1,
    InProgress = 2,
    OnHold = 3,
    Completed = 4,
    Cancelled = 5
}
