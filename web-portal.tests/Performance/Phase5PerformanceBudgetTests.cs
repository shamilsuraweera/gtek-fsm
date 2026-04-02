namespace GTEK.FSM.WebPortal.Tests.Performance;

using System.Diagnostics;
using Bunit;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Components;
using GTEK.FSM.WebPortal.Models;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Management;
using GTEK.FSM.WebPortal.Services.Security;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using Microsoft.Extensions.DependencyInjection;

public sealed class Phase5PerformanceBudgetTests : TestContext
{
    public Phase5PerformanceBudgetTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped<UiSecurityContext>();
        this.Services.AddScoped<IManagementReportsApiClient>(_ => new FakeManagementReportsApiClient());
    }

    [Fact]
    public void AssignmentShell_RenderBudget_WithHighDensityData_StaysWithinThreshold()
    {
        // Arrange
        this.Services.AddScoped<UiSecurityContext>();

        var requests = BuildRequests(500);
        var workers = BuildWorkers(45);

        // Act
        var stopwatch = Stopwatch.StartNew();
        var cut = this.RenderComponent<AssignmentCoordinationShell>(parameters => parameters
            .Add(x => x.Requests, requests)
            .Add(x => x.Workers, workers));
        stopwatch.Stop();

        // Assert: baseline budget for high-density operational shell rendering.
        Assert.True(stopwatch.ElapsedMilliseconds <= 2500, $"Assignment shell render exceeded budget: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(cut.Markup.Length <= 1_500_000, $"Assignment shell markup exceeded budget: {cut.Markup.Length} chars");
    }

    [Fact]
    public void ManagementReports_LoadBudget_StaysWithinThreshold()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var cut = this.RenderComponent<Reports>();
        cut.WaitForAssertion(() => Assert.Contains("Anomaly Indicators", cut.Markup, StringComparison.Ordinal), TimeSpan.FromSeconds(5));
        stopwatch.Stop();

        // Assert: includes fetch delay + render under a practical UI budget.
        Assert.True(stopwatch.ElapsedMilliseconds <= 4000, $"Management reports load exceeded budget: {stopwatch.ElapsedMilliseconds}ms");
    }

    private static List<OperationalQueueItem> BuildRequests(int count)
    {
        var result = new List<OperationalQueueItem>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(new OperationalQueueItem
            {
                Reference = $"ASN-{1000 + i}",
                Customer = $"Customer {i}",
                TenantId = i % 3 == 0 ? "TENANT-01" : "TENANT-02",
                Stage = i % 2 == 0 ? "Dispatch" : "Assessment",
                Priority = i % 5 == 0 ? "Critical" : "High",
                Summary = "Performance baseline request",
                UpdatedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                Status = i % 2 == 0 ? RequestStage.Assigned : RequestStage.OnHold,
                UrgencyLevel = i % 5 == 0 ? UrgencyLevel.Critical : UrgencyLevel.High,
                IsEscalated = i % 7 == 0,
                IsSLABreach = i % 9 == 0,
                AgeMinutes = i,
            });
        }

        return result;
    }

    private static List<AssignmentWorkerOption> BuildWorkers(int count)
    {
        var result = new List<AssignmentWorkerOption>(count);
        for (var i = 0; i < count; i++)
        {
            result.Add(new AssignmentWorkerOption
            {
                Id = $"W-{i}",
                Name = $"Worker {i}",
                SkillTag = i % 2 == 0 ? "Dispatch" : "Electrical",
                ActiveAssignments = i % 6,
                IsAvailable = i % 8 != 0,
                HasConflict = i % 10 == 0,
                ConflictReason = i % 10 == 0 ? "Synthetic conflict for budget test." : null,
            });
        }

        return result;
    }

    private sealed class FakeManagementReportsApiClient : IManagementReportsApiClient
    {
        public Task<GetManagementAnalyticsOverviewResponse> GetOverviewAsync(int? windowDays = null, int? trendBuckets = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetManagementAnalyticsOverviewResponse
            {
                TotalRequestsInWindow = 40,
                CompletedRequestsInWindow = 25,
                ActiveJobs = 12,
                SensitiveActions24h = 11,
                DeniedActions24h = 2,
                IntakeTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.AddDays(-1), Value = 7 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow, Value = 8 },
                ],
                CompletionTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.AddDays(-1), Value = 4 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow, Value = 5 },
                ],
                Anomalies =
                [
                    new ManagementAnomalyIndicatorResponse
                    {
                        Code = "NO_CRITICAL_ANOMALY",
                        Severity = "Low",
                        Message = "No high-risk anomaly indicators detected.",
                    },
                ],
                ActionDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "CATEGORY_UPDATED", Count = 8 },
                ],
                OutcomeDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "Success", Count = 9 },
                ],
            });
        }
    }
}
