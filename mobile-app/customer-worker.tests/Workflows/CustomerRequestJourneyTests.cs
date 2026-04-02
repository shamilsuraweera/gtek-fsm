namespace GTEK.FSM.MobileApp.Tests.Workflows;

using GTEK.FSM.MobileApp.Workflows;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

public sealed class CustomerRequestJourneyTests
{
    [Fact]
    public void PlanSubmission_ReturnsTitle_WhenDraftIsValid()
    {
        var result = CustomerRequestJourney.PlanSubmission("Electrical", "Main panel trips every evening");

        Assert.True(result.IsValid);
        Assert.Equal("Electrical: Main panel trips every evening", result.Title);
        Assert.Equal(string.Empty, result.FeedbackMessage);
    }

    [Theory]
    [InlineData("", "Main panel trips every evening", "Select a category before submitting.")]
    [InlineData("Electrical", "short", "Provide at least 10 characters of details.")]
    public void PlanSubmission_ReturnsValidationFeedback_WhenDraftIsInvalid(string categoryName, string details, string expectedMessage)
    {
        var result = CustomerRequestJourney.PlanSubmission(categoryName, details);

        Assert.False(result.IsValid);
        Assert.Equal(expectedMessage, result.FeedbackMessage);
    }

    [Fact]
    public void ResolvePendingRequestId_MatchesIgnoringCase()
    {
        var requests = new[]
        {
            new CustomerRequestSnapshot("REQ-100", "A", "A", "Updated", "Submitted", 0),
            new CustomerRequestSnapshot("REQ-200", "B", "B", "Updated", "Scheduled", 1),
        };

        var resolved = CustomerRequestJourney.ResolvePendingRequestId("req-200", requests);

        Assert.Equal("REQ-200", resolved);
    }

    [Fact]
    public void SyncDetail_UpdatesLifecycleStageAndEta()
    {
        var request = new CustomerRequestSnapshot("REQ-200", "Cooling issue", "Cooling issue", "Updated old", "Submitted", 0);
        var detail = new GetServiceRequestDetailResponse
        {
            Status = "InProgress",
            UpdatedAtUtc = new DateTime(2026, 4, 2, 12, 15, 0, DateTimeKind.Utc),
        };

        var synced = CustomerRequestJourney.SyncDetail(request, detail);

        Assert.Equal("InProgress", synced.StatusLabel);
        Assert.Equal(2, synced.CurrentStage);
        Assert.Contains("Updated", synced.EtaText);
    }

    [Fact]
    public void BuildDetailPresentation_ReturnsWorkerJobAndTimelineText()
    {
        var detail = new GetServiceRequestDetailResponse
        {
            Status = "Assigned",
            AssignedWorkerUserId = "worker-44",
            ActiveJobId = "JOB-100",
            ActiveJobStatus = "Accepted",
            UpdatedAtUtc = new DateTime(2026, 4, 2, 12, 15, 0, DateTimeKind.Utc),
            Timeline =
            [
                new DetailTimelineItemResponse
                {
                    EventType = "JOB_ASSIGNED",
                    Message = "Assignment accepted.",
                    OccurredAtUtc = new DateTime(2026, 4, 2, 12, 0, 0, DateTimeKind.Utc),
                    ActorUserId = "worker-44",
                },
            ],
        };

        var presentation = CustomerRequestJourney.BuildDetailPresentation(detail);

        Assert.Equal("Current lifecycle: Assigned", presentation.LifecycleText);
        Assert.Equal("Assigned worker ID: worker-44", presentation.WorkerText);
        Assert.Equal("Active job JOB-100 • Status: Accepted", presentation.JobText);
        Assert.Single(presentation.TimelineLines);
        Assert.Contains("JOB_ASSIGNED", presentation.TimelineLines[0]);
    }

    [Fact]
    public void ApplyStatusUpdate_NormalizesStatusAndRefreshesEta()
    {
        var request = new CustomerRequestSnapshot("REQ-200", "Cooling issue", "Cooling issue", "Updated old", "Submitted", 0);
        var payload = new ServiceRequestStatusUpdatedEvent
        {
            RequestId = "REQ-200",
            CurrentStatus = "InProgress",
            UpdatedAtUtc = new DateTime(2026, 4, 2, 13, 45, 0, DateTimeKind.Utc),
        };

        var updated = CustomerRequestJourney.ApplyStatusUpdate(request, payload);

        Assert.Equal("In Progress", updated.StatusLabel);
        Assert.Equal(2, updated.CurrentStage);
        Assert.Contains("Updated", updated.EtaText);
    }
}