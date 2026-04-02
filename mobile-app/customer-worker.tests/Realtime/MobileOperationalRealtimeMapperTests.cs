namespace GTEK.FSM.MobileApp.Tests.Realtime;

using GTEK.FSM.MobileApp.Services.Realtime;

public sealed class MobileOperationalRealtimeMapperTests
{
    [Theory]
    [InlineData("in_progress", "In Progress")]
    [InlineData("on_route", "On Route")]
    [InlineData("accepted", "Accepted")]
    [InlineData("completed", "Completed")]
    [InlineData("", "New")]
    public void NormalizeStatus_MapsBackendPayloadValues(string rawStatus, string expected)
    {
        var normalized = MobileOperationalRealtimeMapper.NormalizeStatus(rawStatus);

        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData("new", 0)]
    [InlineData("scheduled", 1)]
    [InlineData("assigned", 1)]
    [InlineData("in_progress", 2)]
    [InlineData("on_hold", 2)]
    [InlineData("completed", 3)]
    public void ResolveRequestStageIndex_MapsLifecycleToTimeline(string status, int expectedStageIndex)
    {
        var stageIndex = MobileOperationalRealtimeMapper.ResolveRequestStageIndex(status);

        Assert.Equal(expectedStageIndex, stageIndex);
    }

    [Theory]
    [InlineData("Accepted", true)]
    [InlineData("On Route", true)]
    [InlineData("On Site", true)]
    [InlineData("In Progress", true)]
    [InlineData("Completed", true)]
    [InlineData("New", false)]
    public void IsAcceptedStatus_IdentifiesAcceptedJobStates(string status, bool expected)
    {
        var accepted = MobileOperationalRealtimeMapper.IsAcceptedStatus(status);

        Assert.Equal(expected, accepted);
    }
}