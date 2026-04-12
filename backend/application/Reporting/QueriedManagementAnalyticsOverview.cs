namespace GTEK.FSM.Backend.Application.Reporting;

public sealed record QueriedManagementAnalyticsOverview(
    int TotalRequestsInWindow,
    int CompletedRequestsInWindow,
    int ActiveJobs,
    int SensitiveActions24h,
    int DeniedActions24h,
    QueriedDecisioningMetricsOverview DecisioningMetrics,
    QueriedAssignmentQualitySummary AssignmentQuality,
    QueriedWorkforceUtilizationSummary WorkforceUtilization,
    IReadOnlyList<QueriedManagementTrendPoint> IntakeTrend,
    IReadOnlyList<QueriedManagementTrendPoint> CompletionTrend,
    IReadOnlyList<QueriedManagementAnomalyIndicator> Anomalies,
    IReadOnlyList<QueriedManagementDrilldownItem> ActionDrilldown,
    IReadOnlyList<QueriedManagementDrilldownItem> OutcomeDrilldown);

public sealed record QueriedManagementTrendPoint(DateTime DateUtc, int Value);

public sealed record QueriedManagementAnomalyIndicator(string Code, string Severity, string Message);

public sealed record QueriedManagementDrilldownItem(string Key, int Count);

public sealed record QueriedDecisioningMetricsOverview(
    int MatchEvaluationCount,
    decimal AverageMatchLatencyMs,
    decimal P95MatchLatencyMs,
    decimal AverageTopMatchScore,
    decimal HighConfidenceMatchRatePercent,
    IReadOnlyList<QueriedManagementTrendPoint> MatchLatencyTrend,
    QueriedSlaOutcomeSummary SlaOutcomes);

public sealed record QueriedSlaOutcomeSummary(
    int ResponseOnTrack,
    int ResponseAtRisk,
    int ResponseBreached,
    int AssignmentOnTrack,
    int AssignmentAtRisk,
    int AssignmentBreached,
    int CompletionOnTrack,
    int CompletionAtRisk,
    int CompletionBreached,
    int EscalationsAtRiskInWindow,
    int EscalationsBreachedInWindow);

public sealed record QueriedAssignmentQualitySummary(
    int AssignmentEventsInWindow,
    int AcceptedJobs,
    int PendingAcceptanceJobs,
    int RejectedJobs,
    int CancelledJobs,
    int CompletedJobs,
    decimal AcceptanceRatePercent,
    decimal CompletionRatePercent,
    IReadOnlyList<QueriedManagementDrilldownItem> StatusDrilldown);

public sealed record QueriedWorkforceUtilizationSummary(
    int ActiveWorkers,
    int AvailableWorkers,
    int BusyWorkers,
    int UtilizedWorkers,
    int OverloadedWorkers,
    decimal UtilizationRatePercent,
    decimal AverageActiveJobsPerUtilizedWorker,
    decimal AverageInternalRating,
    IReadOnlyList<QueriedManagementDrilldownItem> AvailabilityDrilldown,
    IReadOnlyList<QueriedManagementDrilldownItem> WorkerLoadDrilldown);
