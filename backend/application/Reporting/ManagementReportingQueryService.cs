using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;

namespace GTEK.FSM.Backend.Application.Reporting;

internal sealed class ManagementReportingQueryService : IManagementReportingQueryService
{
    private const int MaxTrendSampleRows = 500;

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IJobRepository jobRepository;
    private readonly IAuditLogRepository auditLogRepository;
    private readonly IDecisioningMetricsCollector decisioningMetricsCollector;

    public ManagementReportingQueryService(
        IServiceRequestRepository serviceRequestRepository,
        IJobRepository jobRepository,
        IAuditLogRepository auditLogRepository,
        IDecisioningMetricsCollector decisioningMetricsCollector)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.jobRepository = jobRepository;
        this.auditLogRepository = auditLogRepository;
        this.decisioningMetricsCollector = decisioningMetricsCollector;
    }

    public async Task<ManagementReportingOverviewQueryResult> GetOverviewAsync(
        AuthenticatedPrincipal principal,
        GetManagementAnalyticsOverviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return ManagementReportingOverviewQueryResult.Failure(
                "Role is not authorized to access management analytics.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var windowDays = Math.Clamp(request.WindowDays ?? 7, 3, 30);
        var trendBuckets = Math.Clamp(request.TrendBuckets ?? 7, 3, 14);

        var nowUtc = DateTime.UtcNow;
        var windowFromUtc = nowUtc.AddDays(-windowDays);

        var totalRequests = await this.serviceRequestRepository.CountAsync(
            new ServiceRequestQuerySpecification(
                TenantId: principal.TenantId,
                CreatedFromUtc: windowFromUtc,
                CreatedToUtc: nowUtc),
            cancellationToken);

        var completedRequests = await this.serviceRequestRepository.CountAsync(
            new ServiceRequestQuerySpecification(
                TenantId: principal.TenantId,
                Status: ServiceRequestStatus.Completed,
                CreatedFromUtc: windowFromUtc,
                CreatedToUtc: nowUtc),
            cancellationToken);

        var activeJobs = await this.jobRepository.CountAsync(
            new JobQuerySpecification(
                TenantId: principal.TenantId,
                AssignmentStatus: AssignmentStatus.Accepted),
            cancellationToken);

        var twentyFourHoursAgo = nowUtc.AddHours(-24);
        var recentAuditSpec = new AuditLogQuerySpecification(
            TenantId: principal.TenantId,
            OccurredFromUtc: twentyFourHoursAgo,
            OccurredToUtc: nowUtc,
            Page: new PageSpecification(1, MaxTrendSampleRows));

        var recentAuditLogs = await this.auditLogRepository.QueryAsync(recentAuditSpec, cancellationToken);
        var sensitiveActions24h = recentAuditLogs.Count;
        var deniedActions24h = recentAuditLogs.Count(x => !string.Equals(x.Outcome, "Success", StringComparison.OrdinalIgnoreCase));

        var intakeTrend = new List<QueriedManagementTrendPoint>(trendBuckets);
        var completionTrend = new List<QueriedManagementTrendPoint>(trendBuckets);

        for (var i = trendBuckets - 1; i >= 0; i--)
        {
            var bucketStart = nowUtc.Date.AddDays(-i);
            var bucketEnd = bucketStart.AddDays(1).AddTicks(-1);

            var intakeCount = await this.serviceRequestRepository.CountAsync(
                new ServiceRequestQuerySpecification(
                    TenantId: principal.TenantId,
                    CreatedFromUtc: bucketStart,
                    CreatedToUtc: bucketEnd),
                cancellationToken);

            var completionCount = await this.auditLogRepository.CountAsync(
                new AuditLogQuerySpecification(
                    TenantId: principal.TenantId,
                    Action: "COMPLETED",
                    OccurredFromUtc: new DateTimeOffset(bucketStart),
                    OccurredToUtc: new DateTimeOffset(bucketEnd)),
                cancellationToken);

            intakeTrend.Add(new QueriedManagementTrendPoint(bucketStart, intakeCount));
            completionTrend.Add(new QueriedManagementTrendPoint(bucketStart, completionCount));
        }

        var windowAuditLogs = await this.auditLogRepository.QueryAsync(
            new AuditLogQuerySpecification(
                TenantId: principal.TenantId,
                OccurredFromUtc: new DateTimeOffset(windowFromUtc),
                OccurredToUtc: new DateTimeOffset(nowUtc),
                Page: new PageSpecification(1, MaxTrendSampleRows)),
            cancellationToken);

        var actionDrilldown = windowAuditLogs
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Action) ? "Unknown" : x.Action)
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .Take(8)
            .ToArray();

        var outcomeDrilldown = windowAuditLogs
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Outcome) ? "Unknown" : x.Outcome)
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        var anomalies = BuildAnomalies(totalRequests, completedRequests, activeJobs, deniedActions24h);
        var decisioningMetrics = await BuildDecisioningMetricsAsync(
            principal.TenantId,
            nowUtc,
            windowFromUtc,
            trendBuckets,
            cancellationToken);

        var payload = new QueriedManagementAnalyticsOverview(
            TotalRequestsInWindow: totalRequests,
            CompletedRequestsInWindow: completedRequests,
            ActiveJobs: activeJobs,
            SensitiveActions24h: sensitiveActions24h,
            DeniedActions24h: deniedActions24h,
            DecisioningMetrics: decisioningMetrics,
            IntakeTrend: intakeTrend,
            CompletionTrend: completionTrend,
            Anomalies: anomalies,
            ActionDrilldown: actionDrilldown,
            OutcomeDrilldown: outcomeDrilldown);

        return ManagementReportingOverviewQueryResult.Success(payload);
    }

    private static IReadOnlyList<QueriedManagementAnomalyIndicator> BuildAnomalies(
        int totalRequests,
        int completedRequests,
        int activeJobs,
        int deniedActions24h)
    {
        var anomalies = new List<QueriedManagementAnomalyIndicator>();

        if (deniedActions24h >= 5)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "DENIED_ACTION_SPIKE",
                "High",
                $"Denied or failed sensitive actions in last 24h: {deniedActions24h}."));
        }

        if (totalRequests > (completedRequests * 2) && totalRequests >= 10)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "REQUEST_BACKLOG_GROWTH",
                "Medium",
                "Request intake is outpacing completion in the selected window."));
        }

        if (activeJobs >= 25)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "ACTIVE_JOB_PRESSURE",
                "Medium",
                $"Active accepted jobs are elevated ({activeJobs})."));
        }

        if (anomalies.Count == 0)
        {
            anomalies.Add(new QueriedManagementAnomalyIndicator(
                "NO_CRITICAL_ANOMALY",
                "Low",
                "No high-risk anomaly indicators detected for the selected window."));
        }

        return anomalies;
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }

    private async Task<QueriedDecisioningMetricsOverview> BuildDecisioningMetricsAsync(
        Guid tenantId,
        DateTime nowUtc,
        DateTime windowFromUtc,
        int trendBuckets,
        CancellationToken cancellationToken)
    {
        var matchSamples = this.decisioningMetricsCollector.GetMatchEvaluations(tenantId, windowFromUtc, nowUtc);
        var matchEvaluationCount = matchSamples.Count;
        var averageLatency = matchEvaluationCount == 0
            ? 0m
            : decimal.Round((decimal)matchSamples.Average(x => x.MatchLatencyMs), 2, MidpointRounding.AwayFromZero);
        var p95Latency = matchEvaluationCount == 0
            ? 0m
            : ComputeP95(matchSamples.Select(x => x.MatchLatencyMs).ToArray());

        var topScores = matchSamples.Where(x => x.TopCandidateScore.HasValue).Select(x => x.TopCandidateScore!.Value).ToArray();
        var averageTopScore = topScores.Length == 0
            ? 0m
            : decimal.Round(topScores.Average(), 4, MidpointRounding.AwayFromZero);

        var highConfidenceRatePercent = matchEvaluationCount == 0
            ? 0m
            : decimal.Round((decimal)(matchSamples.Count(x => x.TopCandidateScore.HasValue && x.TopCandidateScore.Value >= 0.80m) * 100.0 / matchEvaluationCount), 2, MidpointRounding.AwayFromZero);

        var latencyTrend = new List<QueriedManagementTrendPoint>(trendBuckets);
        for (var i = trendBuckets - 1; i >= 0; i--)
        {
            var bucketStart = nowUtc.Date.AddDays(-i);
            var bucketEnd = bucketStart.AddDays(1);
            var bucketSamples = matchSamples
                .Where(x => x.ObservedAtUtc >= bucketStart && x.ObservedAtUtc < bucketEnd)
                .ToArray();

            var bucketValue = bucketSamples.Length == 0
                ? 0
                : (int)Math.Round(bucketSamples.Average(x => x.MatchLatencyMs), MidpointRounding.AwayFromZero);

            latencyTrend.Add(new QueriedManagementTrendPoint(bucketStart, bucketValue));
        }

        var slaOutcomes = await BuildSlaOutcomeSummaryAsync(tenantId, windowFromUtc, nowUtc, cancellationToken);

        return new QueriedDecisioningMetricsOverview(
            MatchEvaluationCount: matchEvaluationCount,
            AverageMatchLatencyMs: averageLatency,
            P95MatchLatencyMs: p95Latency,
            AverageTopMatchScore: averageTopScore,
            HighConfidenceMatchRatePercent: highConfidenceRatePercent,
            MatchLatencyTrend: latencyTrend,
            SlaOutcomes: slaOutcomes);
    }

    private async Task<QueriedSlaOutcomeSummary> BuildSlaOutcomeSummaryAsync(
        Guid tenantId,
        DateTime windowFromUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var responseOnTrack = 0;
        var responseAtRisk = 0;
        var responseBreached = 0;
        var assignmentOnTrack = 0;
        var assignmentAtRisk = 0;
        var assignmentBreached = 0;
        var completionOnTrack = 0;
        var completionAtRisk = 0;
        var completionBreached = 0;

        var page = 1;
        while (true)
        {
            var batch = await this.serviceRequestRepository.QueryAsync(
                new ServiceRequestQuerySpecification(
                    TenantId: tenantId,
                    Page: new PageSpecification(page, 200)),
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            foreach (var item in batch)
            {
                CountSlaState(item.ResponseSlaState, ref responseOnTrack, ref responseAtRisk, ref responseBreached);
                CountSlaState(item.AssignmentSlaState, ref assignmentOnTrack, ref assignmentAtRisk, ref assignmentBreached);
                CountSlaState(item.CompletionSlaState, ref completionOnTrack, ref completionAtRisk, ref completionBreached);
            }

            if (batch.Count < 200)
            {
                break;
            }

            page++;
        }

        var escalationsAtRiskInWindow = 0;
        var escalationsBreachedInWindow = 0;

        var escalationPage = 1;
        while (true)
        {
            var escalationBatch = await this.auditLogRepository.QueryAsync(
                new AuditLogQuerySpecification(
                    TenantId: tenantId,
                    Action: "SlaEscalation:",
                    OccurredFromUtc: new DateTimeOffset(windowFromUtc),
                    OccurredToUtc: new DateTimeOffset(nowUtc),
                    Page: new PageSpecification(escalationPage, 200)),
                cancellationToken);

            if (escalationBatch.Count == 0)
            {
                break;
            }

            escalationsAtRiskInWindow += escalationBatch.Count(x => x.Action.EndsWith(":AtRisk", StringComparison.OrdinalIgnoreCase));
            escalationsBreachedInWindow += escalationBatch.Count(x => x.Action.EndsWith(":Breached", StringComparison.OrdinalIgnoreCase));

            if (escalationBatch.Count < 200)
            {
                break;
            }

            escalationPage++;
        }

        return new QueriedSlaOutcomeSummary(
            ResponseOnTrack: responseOnTrack,
            ResponseAtRisk: responseAtRisk,
            ResponseBreached: responseBreached,
            AssignmentOnTrack: assignmentOnTrack,
            AssignmentAtRisk: assignmentAtRisk,
            AssignmentBreached: assignmentBreached,
            CompletionOnTrack: completionOnTrack,
            CompletionAtRisk: completionAtRisk,
            CompletionBreached: completionBreached,
            EscalationsAtRiskInWindow: escalationsAtRiskInWindow,
            EscalationsBreachedInWindow: escalationsBreachedInWindow);
    }

    private static decimal ComputeP95(long[] values)
    {
        if (values.Length == 0)
        {
            return 0m;
        }

        Array.Sort(values);
        var percentileIndex = (int)Math.Ceiling(values.Length * 0.95d) - 1;
        percentileIndex = Math.Clamp(percentileIndex, 0, values.Length - 1);
        return values[percentileIndex];
    }

    private static void CountSlaState(SlaState state, ref int onTrack, ref int atRisk, ref int breached)
    {
        switch (state)
        {
            case SlaState.OnTrack:
                onTrack++;
                break;
            case SlaState.AtRisk:
                atRisk++;
                break;
            case SlaState.Breached:
                breached++;
                break;
        }
    }
}
