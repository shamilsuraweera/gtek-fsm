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
        var continuousImprovement = BuildContinuousImprovementOverview(
            windowDays,
            nowUtc,
            totalRequests,
            completedRequests,
            deniedActions24h,
            decisioningMetrics,
            assignmentQuality,
            workforceUtilization,
            anomalies);

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
            OutcomeDrilldown: outcomeDrilldown,
            ContinuousImprovement: continuousImprovement);

        return ManagementReportingOverviewQueryResult.Success(payload);
    }

    private static QueriedContinuousImprovementOverview BuildContinuousImprovementOverview(
        int windowDays,
        DateTime nowUtc,
        int totalRequests,
        int completedRequests,
        int deniedActions24h,
        QueriedDecisioningMetricsOverview decisioningMetrics,
        QueriedAssignmentQualitySummary assignmentQuality,
        QueriedWorkforceUtilizationSummary workforceUtilization,
        IReadOnlyList<QueriedManagementAnomalyIndicator> anomalies)
    {
        var items = new List<QueriedContinuousImprovementItem>();
        var completionRatePercent = totalRequests == 0
            ? 100m
            : decimal.Round(completedRequests * 100m / totalRequests, 2, MidpointRounding.AwayFromZero);

        if (completionRatePercent < 70m)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "COMPLETION_RECOVERY",
                Priority: "High",
                Metric: "Completion rate",
                CurrentState: $"{completionRatePercent:0.##}% completed in the active review window.",
                TargetState: "Maintain at least 70% completion throughput in-window.",
                RecommendedAction: "Review incomplete-request backlog, triage the oldest blocked work, and add one recovery item to the next operations backlog.",
                ReviewOwner: "Operations Manager"));
        }

        if (assignmentQuality.AssignmentEventsInWindow > 0 && assignmentQuality.AcceptanceRatePercent < 80m)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "ASSIGNMENT_ACCEPTANCE_TUNING",
                Priority: "High",
                Metric: "Assignment acceptance rate",
                CurrentState: $"{assignmentQuality.AcceptanceRatePercent:0.##}% across {assignmentQuality.AssignmentEventsInWindow} assignment events.",
                TargetState: "Maintain at least 80% assignment acceptance.",
                RecommendedAction: "Inspect rejected and pending assignment bands, then refine dispatch rules or worker availability coverage before the next review.",
                ReviewOwner: "Dispatch Lead"));
        }

        if (assignmentQuality.AssignmentEventsInWindow > 0 && assignmentQuality.CompletionRatePercent < 65m)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "JOB_COMPLETION_FOLLOW_THROUGH",
                Priority: "High",
                Metric: "Job completion rate",
                CurrentState: $"{assignmentQuality.CompletionRatePercent:0.##}% completed from {assignmentQuality.AssignmentEventsInWindow} assignment events.",
                TargetState: "Maintain at least 65% completion conversion from assignments.",
                RecommendedAction: "Review completion blockers, rework overdue handoffs, and schedule a targeted follow-up on the failing workflow segment.",
                ReviewOwner: "Field Operations Lead"));
        }

        if (decisioningMetrics.SlaOutcomes.CompletionBreached > 0 || decisioningMetrics.SlaOutcomes.EscalationsBreachedInWindow > 0)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "SLA_BREACH_RECOVERY",
                Priority: "High",
                Metric: "Completion SLA health",
                CurrentState: $"{decisioningMetrics.SlaOutcomes.CompletionBreached} completion breaches and {decisioningMetrics.SlaOutcomes.EscalationsBreachedInWindow} breached escalations in window.",
                TargetState: "Zero breached completion SLAs in the active review cycle.",
                RecommendedAction: "Open a recovery action for the breached workflow path and verify escalation triggers, staffing coverage, and handoff latency.",
                ReviewOwner: "Service Delivery Manager"));
        }

        if (workforceUtilization.OverloadedWorkers > 0 || workforceUtilization.UtilizationRatePercent > 85m)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "CAPACITY_REBALANCE",
                Priority: "Medium",
                Metric: "Workforce utilization",
                CurrentState: $"{workforceUtilization.UtilizationRatePercent:0.##}% utilized with {workforceUtilization.OverloadedWorkers} overloaded workers.",
                TargetState: "Keep utilization below 85% and eliminate overloaded worker pockets.",
                RecommendedAction: "Rebalance active workload, review worker availability drift, and prepare a staffing or routing adjustment for the next cycle.",
                ReviewOwner: "Workforce Manager"));
        }

        if (decisioningMetrics.MatchEvaluationCount > 0 && (decisioningMetrics.HighConfidenceMatchRatePercent < 75m || decisioningMetrics.P95MatchLatencyMs > 250m))
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "DECISIONING_SIGNAL_TUNING",
                Priority: "Medium",
                Metric: "Decisioning quality",
                CurrentState: $"{decisioningMetrics.HighConfidenceMatchRatePercent:0.##}% high-confidence matches, p95 latency {decisioningMetrics.P95MatchLatencyMs:0.##} ms.",
                TargetState: "Maintain at least 75% high-confidence matches and keep p95 latency at or below 250 ms.",
                RecommendedAction: "Review scoring inputs, low-confidence match cases, and latency spikes before adjusting matching weights or cache strategy.",
                ReviewOwner: "Platform Optimization Lead"));
        }

        if (deniedActions24h >= 3)
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "GOVERNANCE_ACCESS_REVIEW",
                Priority: "Medium",
                Metric: "Denied sensitive actions",
                CurrentState: $"{deniedActions24h} denied or failed sensitive actions recorded in the last 24 hours.",
                TargetState: "Keep denied sensitive actions below 3 per 24-hour review window.",
                RecommendedAction: "Review role misuse, permission drift, and operator guidance gaps, then create one governance corrective action if the pattern persists.",
                ReviewOwner: "Security and Governance Lead"));
        }

        if (anomalies.Any(x => string.Equals(x.Code, "ACTIVE_JOB_PRESSURE", StringComparison.Ordinal))
            && items.All(x => !string.Equals(x.Code, "CAPACITY_REBALANCE", StringComparison.Ordinal)))
        {
            items.Add(new QueriedContinuousImprovementItem(
                Code: "ACTIVE_JOB_PRESSURE_REVIEW",
                Priority: "Medium",
                Metric: "Active job pressure",
                CurrentState: "Accepted active jobs remain elevated for the selected review window.",
                TargetState: "Reduce active-job pressure back into standard operating range.",
                RecommendedAction: "Review intake-to-assignment lag, identify stalled active jobs, and escalate one throughput improvement item into the next backlog.",
                ReviewOwner: "Operations Manager"));
        }

        var prioritizedItems = items
            .OrderBy(x => PriorityRank(x.Priority))
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToArray();

        return new QueriedContinuousImprovementOverview(
            CadenceName: "Weekly KPI Review",
            ReviewWindowDays: windowDays,
            NextReviewOnUtc: nowUtc.Date.AddDays(7),
            PrioritizationRule: "High items become immediate backlog candidates; Medium items require planned follow-up in the next review cycle; Low items remain watchlist signals.",
            ImprovementItems: prioritizedItems);
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

    private static int PriorityRank(string priority)
    {
        return priority switch
        {
            "High" => 0,
            "Medium" => 1,
            _ => 2,
        };
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
