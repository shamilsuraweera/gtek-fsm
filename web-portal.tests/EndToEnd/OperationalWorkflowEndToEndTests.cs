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
            Assert.Contains("Continuous Improvement Cadence", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("SLA_BREACH_RECOVERY", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("CATEGORY_UPDATED", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Assignment Quality", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Worker Utilization", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Availability and Load", cut.Markup, StringComparison.Ordinal);
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
                AssignmentQuality = new ManagementAssignmentQualitySummaryResponse
                {
                    AssignmentEventsInWindow = 6,
                    AcceptedJobs = 4,
                    PendingAcceptanceJobs = 1,
                    RejectedJobs = 1,
                    CancelledJobs = 0,
                    CompletedJobs = 3,
                    AcceptanceRatePercent = 66.67m,
                    CompletionRatePercent = 50m,
                    StatusDrilldown =
                    [
                        new ManagementDrilldownItemResponse { Key = "Accepted", Count = 4 },
                        new ManagementDrilldownItemResponse { Key = "PendingAcceptance", Count = 1 },
                    ],
                },
                WorkforceUtilization = new ManagementWorkforceUtilizationSummaryResponse
                {
                    ActiveWorkers = 5,
                    AvailableWorkers = 2,
                    BusyWorkers = 3,
                    UtilizedWorkers = 3,
                    OverloadedWorkers = 1,
                    UtilizationRatePercent = 60m,
                    AverageActiveJobsPerUtilizedWorker = 1.33m,
                    AverageInternalRating = 4.42m,
                    AvailabilityDrilldown =
                    [
                        new ManagementDrilldownItemResponse { Key = "Busy", Count = 3 },
                        new ManagementDrilldownItemResponse { Key = "Available", Count = 2 },
                    ],
                    WorkerLoadDrilldown =
                    [
                        new ManagementDrilldownItemResponse { Key = "SingleActiveJob", Count = 2 },
                        new ManagementDrilldownItemResponse { Key = "MultiActiveJobs", Count = 1 },
                    ],
                },
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
                ContinuousImprovement = new ManagementContinuousImprovementResponse
                {
                    CadenceName = "Weekly KPI Review",
                    ReviewWindowDays = 7,
                    NextReviewOnUtc = DateTime.UtcNow.AddDays(7),
                    PrioritizationRule = "High items become immediate backlog candidates.",
                    ImprovementItems =
                    [
                        new ManagementImprovementItemResponse
                        {
                            Code = "SLA_BREACH_RECOVERY",
                            Priority = "High",
                            Metric = "Completion SLA health",
                            CurrentState = "2 completion breaches were recorded in the active review window.",
                            TargetState = "Zero breached completion SLAs.",
                            RecommendedAction = "Open a recovery backlog item for the breached workflow.",
                            ReviewOwner = "Service Delivery Manager",
                        },
                    ],
                },
            });
        }
    }
}
