namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;

public sealed class GetManagementAnalyticsOverviewResponse
{
    public int TotalRequestsInWindow { get; set; }

    public int CompletedRequestsInWindow { get; set; }

    public int ActiveJobs { get; set; }

    public int SensitiveActions24h { get; set; }

    public int DeniedActions24h { get; set; }

    public ManagementDecisioningMetricsResponse DecisioningMetrics { get; set; } = new();

    public ManagementAssignmentQualitySummaryResponse AssignmentQuality { get; set; } = new();

    public ManagementWorkforceUtilizationSummaryResponse WorkforceUtilization { get; set; } = new();

    public IReadOnlyList<ManagementTrendPointResponse> IntakeTrend { get; set; } = Array.Empty<ManagementTrendPointResponse>();

    public IReadOnlyList<ManagementTrendPointResponse> CompletionTrend { get; set; } = Array.Empty<ManagementTrendPointResponse>();

    public IReadOnlyList<ManagementAnomalyIndicatorResponse> Anomalies { get; set; } = Array.Empty<ManagementAnomalyIndicatorResponse>();

    public IReadOnlyList<ManagementDrilldownItemResponse> ActionDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();

    public IReadOnlyList<ManagementDrilldownItemResponse> OutcomeDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();
}

public sealed class ManagementDecisioningMetricsResponse
{
    public int MatchEvaluationCount { get; set; }

    public decimal AverageMatchLatencyMs { get; set; }

    public decimal P95MatchLatencyMs { get; set; }

    public decimal AverageTopMatchScore { get; set; }

    public decimal HighConfidenceMatchRatePercent { get; set; }

    public IReadOnlyList<ManagementTrendPointResponse> MatchLatencyTrend { get; set; } = Array.Empty<ManagementTrendPointResponse>();

    public ManagementSlaOutcomeSummaryResponse SlaOutcomes { get; set; } = new();
}

public sealed class ManagementSlaOutcomeSummaryResponse
{
    public int ResponseOnTrack { get; set; }

    public int ResponseAtRisk { get; set; }

    public int ResponseBreached { get; set; }

    public int AssignmentOnTrack { get; set; }

    public int AssignmentAtRisk { get; set; }

    public int AssignmentBreached { get; set; }

    public int CompletionOnTrack { get; set; }

    public int CompletionAtRisk { get; set; }

    public int CompletionBreached { get; set; }

    public int EscalationsAtRiskInWindow { get; set; }

    public int EscalationsBreachedInWindow { get; set; }
}

public sealed class ManagementAssignmentQualitySummaryResponse
{
    public int AssignmentEventsInWindow { get; set; }

    public int AcceptedJobs { get; set; }

    public int PendingAcceptanceJobs { get; set; }

    public int RejectedJobs { get; set; }

    public int CancelledJobs { get; set; }

    public int CompletedJobs { get; set; }

    public decimal AcceptanceRatePercent { get; set; }

    public decimal CompletionRatePercent { get; set; }

    public IReadOnlyList<ManagementDrilldownItemResponse> StatusDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();
}

public sealed class ManagementWorkforceUtilizationSummaryResponse
{
    public int ActiveWorkers { get; set; }

    public int AvailableWorkers { get; set; }

    public int BusyWorkers { get; set; }

    public int UtilizedWorkers { get; set; }

    public int OverloadedWorkers { get; set; }

    public decimal UtilizationRatePercent { get; set; }

    public decimal AverageActiveJobsPerUtilizedWorker { get; set; }

    public decimal AverageInternalRating { get; set; }

    public IReadOnlyList<ManagementDrilldownItemResponse> AvailabilityDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();

    public IReadOnlyList<ManagementDrilldownItemResponse> WorkerLoadDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();
}

public sealed class ManagementTrendPointResponse
{
    public DateTime DateUtc { get; set; }

    public int Value { get; set; }
}

public sealed class ManagementAnomalyIndicatorResponse
{
    public string Code { get; set; } = string.Empty;

    public string Severity { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

public sealed class ManagementDrilldownItemResponse
{
    public string Key { get; set; } = string.Empty;

    public int Count { get; set; }
}
