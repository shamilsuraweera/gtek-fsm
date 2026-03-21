namespace GTEK.FSM.WebPortal.Tests.EndToEnd;

using Bunit;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Security;
using Microsoft.Extensions.DependencyInjection;

public sealed class OperationalWorkflowEndToEndTests : TestContext
{
    public OperationalWorkflowEndToEndTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped<UiSecurityContext>();
    }

    [Fact]
    public void ReportsWorkflow_AcknowledgeReview_RemovesQueueItem_AndAddsAuditEvent()
    {
        // Arrange
        var cut = this.RenderComponent<Reports>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Oversight Review Queue", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        var initialReviewRows = cut.FindAll(".oversight-item").Count;

        // Act: acknowledge the first enabled review action.
        var acknowledgeButton = cut
            .FindAll("button")
            .First(x => x.TextContent.Contains("Acknowledge", StringComparison.Ordinal) && !x.HasAttribute("disabled"));

        acknowledgeButton.Click();

        // Assert: one review is consumed and an audit event is appended.
        cut.WaitForAssertion(() =>
        {
            var currentReviewRows = cut.FindAll(".oversight-item").Count;
            Assert.Equal(initialReviewRows - 1, currentReviewRows);
            Assert.Contains("Oversight review Acknowledge", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }
}
