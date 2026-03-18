using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Policies;
using Xunit;

namespace GTEK.FSM.Backend.Domain.Tests.Policies;

public class WorkerAvailabilityTransitionTests
{
    [Fact]
    public void CanTransition_OfflineToAvailable_ReturnsTrue()
    {
        var canTransition = WorkerAvailabilityTransitions.CanTransition(
            WorkerAvailabilityStatus.Offline,
            WorkerAvailabilityStatus.Available);

        Assert.True(canTransition);
    }

    [Fact]
    public void CanTransition_OfflineToBusy_ReturnsFalse()
    {
        var canTransition = WorkerAvailabilityTransitions.CanTransition(
            WorkerAvailabilityStatus.Offline,
            WorkerAvailabilityStatus.Busy);

        Assert.False(canTransition);
    }
}
