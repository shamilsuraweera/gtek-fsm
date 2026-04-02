namespace GTEK.FSM.WebPortal.Tests.EndToEnd;

using Bunit;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Management;
using GTEK.FSM.WebPortal.Services.Security;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using Microsoft.Extensions.DependencyInjection;

public sealed class OperationalWorkflowEndToEndTests : TestContext
{
    public OperationalWorkflowEndToEndTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped<UiSecurityContext>();
        this.Services.AddScoped<IManagementReportsApiClient>(_ => new FakeManagementReportsApiClient());
    }

    [Fact]
    public void ReportsWorkflow_RendersAnalyticsAnomalies_AndDrilldownRows()
    {
        // Arrange
        var cut = this.RenderComponent<Reports>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Anomaly Indicators", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));

        // Act
        cut.FindAll("button").First(x => x.TextContent.Contains("Refresh", StringComparison.Ordinal)).Click();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("DENIED_ACTION_SPIKE", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("CATEGORY_UPDATED", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    private sealed class FakeManagementReportsApiClient : IManagementReportsApiClient
    {
        public Task<GetManagementAnalyticsOverviewResponse> GetOverviewAsync(int? windowDays = null, int? trendBuckets = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetManagementAnalyticsOverviewResponse
            {
                TotalRequestsInWindow = 16,
                CompletedRequestsInWindow = 9,
                ActiveJobs = 4,
                SensitiveActions24h = 8,
                DeniedActions24h = 3,
                IntakeTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.AddDays(-1), Value = 3 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow, Value = 4 },
                ],
                CompletionTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.AddDays(-1), Value = 2 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow, Value = 1 },
                ],
                Anomalies =
                [
                    new ManagementAnomalyIndicatorResponse
                    {
                        Code = "DENIED_ACTION_SPIKE",
                        Severity = "High",
                        Message = "Denied actions increased.",
                    },
                ],
                ActionDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "CATEGORY_UPDATED", Count = 5 },
                ],
                OutcomeDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "Success", Count = 6 },
                ],
            });
        }
    }
}
