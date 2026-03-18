using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Domain.Policies;

/// <summary>
/// Allowed transition policy for job assignment lifecycle.
/// </summary>
public static class AssignmentStateTransitions
{
    public static bool CanTransition(AssignmentStatus from, AssignmentStatus to)
    {
        return (from, to) switch
        {
            (AssignmentStatus.Unassigned, AssignmentStatus.PendingAcceptance) => true,
            (AssignmentStatus.Unassigned, AssignmentStatus.Cancelled) => true,
            (AssignmentStatus.PendingAcceptance, AssignmentStatus.Accepted) => true,
            (AssignmentStatus.PendingAcceptance, AssignmentStatus.Rejected) => true,
            (AssignmentStatus.PendingAcceptance, AssignmentStatus.Cancelled) => true,
            (AssignmentStatus.Rejected, AssignmentStatus.PendingAcceptance) => true,
            (AssignmentStatus.Accepted, AssignmentStatus.Completed) => true,
            (AssignmentStatus.Accepted, AssignmentStatus.Cancelled) => true,
            (AssignmentStatus.Completed, AssignmentStatus.Completed) => true,
            (AssignmentStatus.Cancelled, AssignmentStatus.Cancelled) => true,
            _ => false
        };
    }
}
