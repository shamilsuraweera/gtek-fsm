using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;

namespace GTEK.FSM.Backend.Application.Reporting;

internal sealed class ManagementReportingQueryService : IManagementReportingQueryService
{
    private const int MaxTrendSampleRows = 500;

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IJobRepository jobRepository;
    private readonly IWorkerProfileRepository workerProfileRepository;
    private readonly IAuditLogRepository auditLogRepository;
    private readonly IDecisioningMetricsCollector decisioningMetricsCollector;

    public ManagementReportingQueryService(
        IServiceRequestRepository serviceRequestRepository,
        IJobRepository jobRepository,
        IWorkerProfileRepository workerProfileRepository,
        IAuditLogRepository auditLogRepository,
        IDecisioningMetricsCollector decisioningMetricsCollector)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.jobRepository = jobRepository;
        this.workerProfileRepository = workerProfileRepository;
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
        var assignmentQuality = await BuildAssignmentQualitySummaryAsync(
            principal.TenantId,
            windowFromUtc,
            nowUtc,
            cancellationToken);
        var workforceUtilization = await BuildWorkforceUtilizationSummaryAsync(
            principal.TenantId,
            cancellationToken);

        var payload = new QueriedManagementAnalyticsOverview(
            TotalRequestsInWindow: totalRequests,
            CompletedRequestsInWindow: completedRequests,
            ActiveJobs: activeJobs,
            SensitiveActions24h: sensitiveActions24h,
            DeniedActions24h: deniedActions24h,
            DecisioningMetrics: decisioningMetrics,
            AssignmentQuality: assignmentQuality,
            WorkforceUtilization: workforceUtilization,
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

    private async Task<QueriedAssignmentQualitySummary> BuildAssignmentQualitySummaryAsync(
        Guid tenantId,
        DateTime windowFromUtc,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var jobs = await LoadAllJobsAsync(tenantId, cancellationToken);
        var jobsInWindow = jobs
            .Where(x => x.CreatedAtUtc >= windowFromUtc && x.CreatedAtUtc <= nowUtc)
            .ToArray();

        var assignmentEventsInWindow = await this.auditLogRepository.CountAsync(
            new AuditLogQuerySpecification(
                TenantId: tenantId,
                Action: "AssignWorker:",
                OccurredFromUtc: new DateTimeOffset(windowFromUtc),
                OccurredToUtc: new DateTimeOffset(nowUtc)),
            cancellationToken);

        var acceptedJobs = jobsInWindow.Count(x => x.AssignmentStatus == AssignmentStatus.Accepted);
        var pendingAcceptanceJobs = jobsInWindow.Count(x => x.AssignmentStatus == AssignmentStatus.PendingAcceptance);
        var rejectedJobs = jobsInWindow.Count(x => x.AssignmentStatus == AssignmentStatus.Rejected);
        var cancelledJobs = jobsInWindow.Count(x => x.AssignmentStatus == AssignmentStatus.Cancelled);
        var completedJobs = jobsInWindow.Count(x => x.AssignmentStatus == AssignmentStatus.Completed);

        var denominator = Math.Max(1, assignmentEventsInWindow);
        var acceptanceRatePercent = decimal.Round(acceptedJobs * 100m / denominator, 2, MidpointRounding.AwayFromZero);
        var completionRatePercent = decimal.Round(completedJobs * 100m / denominator, 2, MidpointRounding.AwayFromZero);

        IReadOnlyList<QueriedManagementDrilldownItem> statusDrilldown =
        [
            new QueriedManagementDrilldownItem("Accepted", acceptedJobs),
            new QueriedManagementDrilldownItem("PendingAcceptance", pendingAcceptanceJobs),
            new QueriedManagementDrilldownItem("Rejected", rejectedJobs),
            new QueriedManagementDrilldownItem("Cancelled", cancelledJobs),
            new QueriedManagementDrilldownItem("Completed", completedJobs),
        ];

        return new QueriedAssignmentQualitySummary(
            AssignmentEventsInWindow: assignmentEventsInWindow,
            AcceptedJobs: acceptedJobs,
            PendingAcceptanceJobs: pendingAcceptanceJobs,
            RejectedJobs: rejectedJobs,
            CancelledJobs: cancelledJobs,
            CompletedJobs: completedJobs,
            AcceptanceRatePercent: acceptanceRatePercent,
            CompletionRatePercent: completionRatePercent,
            StatusDrilldown: statusDrilldown);
    }

    private async Task<QueriedWorkforceUtilizationSummary> BuildWorkforceUtilizationSummaryAsync(
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var workers = await LoadAllWorkersAsync(tenantId, cancellationToken);
        var activeWorkers = workers.Where(x => x.IsActive).ToArray();
        var activeWorkerIds = activeWorkers.Select(x => x.Id).ToArray();

        var activeJobCounts = activeWorkerIds.Length == 0
            ? new Dictionary<Guid, int>()
            : new Dictionary<Guid, int>(await this.jobRepository.GetActiveJobCountsByWorkerAsync(tenantId, activeWorkerIds, cancellationToken));

        var availableWorkers = activeWorkers.Count(x => x.AvailabilityStatus == WorkerAvailabilityStatus.Available);
        var busyWorkers = activeWorkers.Count(x => x.AvailabilityStatus == WorkerAvailabilityStatus.Busy);
        var utilizedWorkers = activeWorkers.Count(x => activeJobCounts.TryGetValue(x.Id, out var count) && count > 0);
        var overloadedWorkers = activeWorkers.Count(x => activeJobCounts.TryGetValue(x.Id, out var count) && count >= 2);
        var utilizationRatePercent = activeWorkers.Length == 0
            ? 0m
            : decimal.Round(utilizedWorkers * 100m / activeWorkers.Length, 2, MidpointRounding.AwayFromZero);
        var averageActiveJobsPerUtilizedWorker = utilizedWorkers == 0
            ? 0m
            : decimal.Round((decimal)activeJobCounts.Values.Where(x => x > 0).Average(), 2, MidpointRounding.AwayFromZero);
        var averageInternalRating = activeWorkers.Length == 0
            ? 0m
            : decimal.Round(activeWorkers.Average(x => x.InternalRating), 2, MidpointRounding.AwayFromZero);

        var availabilityDrilldown = activeWorkers
            .GroupBy(x => x.AvailabilityStatus.ToString())
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        var workerLoadDrilldown = activeWorkers
            .Select(x => activeJobCounts.TryGetValue(x.Id, out var count)
                ? count switch
                {
                    0 => "Idle",
                    1 => "SingleActiveJob",
                    _ => "MultiActiveJobs",
                }
                : "Idle")
            .GroupBy(x => x)
            .Select(x => new QueriedManagementDrilldownItem(x.Key, x.Count()))
            .OrderByDescending(x => x.Count)
            .ToArray();

        return new QueriedWorkforceUtilizationSummary(
            ActiveWorkers: activeWorkers.Length,
            AvailableWorkers: availableWorkers,
            BusyWorkers: busyWorkers,
            UtilizedWorkers: utilizedWorkers,
            OverloadedWorkers: overloadedWorkers,
            UtilizationRatePercent: utilizationRatePercent,
            AverageActiveJobsPerUtilizedWorker: averageActiveJobsPerUtilizedWorker,
            AverageInternalRating: averageInternalRating,
            AvailabilityDrilldown: availabilityDrilldown,
            WorkerLoadDrilldown: workerLoadDrilldown);
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

    private async Task<IReadOnlyList<Job>> LoadAllJobsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var jobs = new List<Job>();
        var page = 1;

        while (true)
        {
            var batch = await this.jobRepository.QueryAsync(
                new JobQuerySpecification(
                    TenantId: tenantId,
                    Page: new PageSpecification(page, 200)),
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            jobs.AddRange(batch);

            if (batch.Count < 200)
            {
                break;
            }

            page++;
        }

        return jobs;
    }

    private async Task<IReadOnlyList<WorkerProfile>> LoadAllWorkersAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var workers = new List<WorkerProfile>();
        var page = 1;

        while (true)
        {
            var batch = await this.workerProfileRepository.QueryAsync(
                new WorkerProfileQuerySpecification(
                    TenantId: tenantId,
                    IncludeInactive: true,
                    Page: new PageSpecification(page, 200)),
                cancellationToken);

            if (batch.Count == 0)
            {
                break;
            }

            workers.AddRange(batch);

            if (batch.Count < 200)
            {
                break;
            }

            page++;
        }

        return workers;
    }
}
