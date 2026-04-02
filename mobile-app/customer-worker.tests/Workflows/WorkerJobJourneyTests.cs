namespace GTEK.FSM.MobileApp.Tests.Workflows;

using GTEK.FSM.MobileApp.Workflows;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

public sealed class WorkerJobJourneyTests
{
    [Fact]
    public void ResolvePendingJobId_PrefersJobIdOverRequestId()
    {
        var jobs = new[]
        {
            new WorkerJobSnapshot("JOB-100", "REQ-100", "Cooling issue", "Cooling issue", "Assigned", false, string.Empty),
            new WorkerJobSnapshot("JOB-200", "REQ-200", "Pump issue", "Pump issue", "Assigned", false, string.Empty),
        };

        var resolved = WorkerJobJourney.ResolvePendingJobId("job-200", "REQ-100", jobs);

        Assert.Equal("JOB-200", resolved);
    }

    [Fact]
    public void MergeExecutionContext_CombinesJobAndRequestDetailState()
    {
        var current = new WorkerJobSnapshot("JOB-200", "REQ-200", "Pump issue", "Pump issue", "Assigned", false, string.Empty);
        var jobDetail = new GetJobDetailResponse
        {
            RequestId = "REQ-200",
            RequestTitle = "Warehouse cooling failure",
            RequestStatus = "Assigned",
        };
        var requestDetail = new GetServiceRequestDetailResponse
        {
            RequestId = "REQ-200",
            Status = "InProgress",
            RowVersion = "AQID",
        };

        var merged = WorkerJobJourney.MergeExecutionContext(current, jobDetail, requestDetail);

        Assert.Equal("REQ-200", merged.RequestId);
        Assert.Equal("Warehouse cooling failure", merged.Title);
        Assert.Equal("Warehouse cooling failure", merged.Description);
        Assert.Equal("In Progress", merged.StatusLabel);
        Assert.True(merged.Accepted);
        Assert.Equal("AQID", merged.RequestRowVersion);
    }

    [Fact]
    public void ApplyTransition_UpdatesStatusAcceptanceAndRowVersion()
    {
        var current = new WorkerJobSnapshot("JOB-200", "REQ-200", "Pump issue", "Pump issue", "Assigned", false, "AQID");
        var transition = new TransitionServiceRequestStatusResponse
        {
            CurrentStatus = "Completed",
            RowVersion = "BAUG",
        };

        var updated = WorkerJobJourney.ApplyTransition(current, transition);

        Assert.Equal("Completed", updated.StatusLabel);
        Assert.True(updated.Accepted);
        Assert.Equal("BAUG", updated.RequestRowVersion);
    }

    [Theory]
    [InlineData(true, "Concurrency conflict detected.", "Conflict detected: Concurrency conflict detected.. Refreshing latest state.")]
    [InlineData(false, "Validation failed.", "Status update failed: Validation failed.")]
    public void BuildTransitionFailureMessage_ReturnsExpectedCopy(bool isConflict, string message, string expected)
    {
        var result = WorkerJobJourney.BuildTransitionFailureMessage(isConflict, message);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Assigned", "Assigned")]
    [InlineData("In Progress", "InProgress")]
    [InlineData("Completed", "Completed")]
    [InlineData("Unknown", "Assigned")]
    public void ToApiStatus_ReturnsExpectedStatus(string status, string expected)
    {
        var result = WorkerJobJourney.ToApiStatus(status);

        Assert.Equal(expected, result);
    }
}