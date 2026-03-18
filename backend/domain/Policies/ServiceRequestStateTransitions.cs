using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Policies;

/// <summary>
/// Allowed transition policy for service request lifecycle.
/// </summary>
public static class ServiceRequestStateTransitions
{
    public static bool CanTransition(ServiceRequestStatus from, ServiceRequestStatus to)
    {
        return (from, to) switch
        {
            (ServiceRequestStatus.New, ServiceRequestStatus.Assigned) => true,
            (ServiceRequestStatus.New, ServiceRequestStatus.Cancelled) => true,
            (ServiceRequestStatus.Assigned, ServiceRequestStatus.InProgress) => true,
            (ServiceRequestStatus.Assigned, ServiceRequestStatus.OnHold) => true,
            (ServiceRequestStatus.Assigned, ServiceRequestStatus.Cancelled) => true,
            (ServiceRequestStatus.InProgress, ServiceRequestStatus.OnHold) => true,
            (ServiceRequestStatus.InProgress, ServiceRequestStatus.Completed) => true,
            (ServiceRequestStatus.InProgress, ServiceRequestStatus.Cancelled) => true,
            (ServiceRequestStatus.OnHold, ServiceRequestStatus.Assigned) => true,
            (ServiceRequestStatus.OnHold, ServiceRequestStatus.InProgress) => true,
            (ServiceRequestStatus.OnHold, ServiceRequestStatus.Cancelled) => true,
            (ServiceRequestStatus.Completed, ServiceRequestStatus.Completed) => true,
            (ServiceRequestStatus.Cancelled, ServiceRequestStatus.Cancelled) => true,
            _ => false
        };
    }
}
