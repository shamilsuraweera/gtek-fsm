using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;
using Xunit;

namespace GTEK.FSM.Backend.Domain.Tests.Policies;

public class CriticalTransitionPolicyTests
{
    [Theory]
    [InlineData(ServiceRequestStatus.New, ServiceRequestStatus.Completed, false)]
    [InlineData(ServiceRequestStatus.Assigned, ServiceRequestStatus.InProgress, true)]
    [InlineData(ServiceRequestStatus.Completed, ServiceRequestStatus.InProgress, false)]
    [InlineData(ServiceRequestStatus.Cancelled, ServiceRequestStatus.New, false)]
    public void ServiceRequestPolicy_TransitionsMatchExpected(ServiceRequestStatus from, ServiceRequestStatus to, bool expected)
    {
        var actual = ServiceRequestStateTransitions.CanTransition(from, to);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(AssignmentStatus.Unassigned, AssignmentStatus.PendingAcceptance, true)]
    [InlineData(AssignmentStatus.Accepted, AssignmentStatus.PendingAcceptance, false)]
    [InlineData(AssignmentStatus.PendingAcceptance, AssignmentStatus.Accepted, true)]
    [InlineData(AssignmentStatus.Completed, AssignmentStatus.Cancelled, false)]
    public void AssignmentPolicy_TransitionsMatchExpected(AssignmentStatus from, AssignmentStatus to, bool expected)
    {
        var actual = AssignmentStateTransitions.CanTransition(from, to);
        Assert.Equal(expected, actual);
    }
}
