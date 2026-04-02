namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;

public sealed class GetManagementAnalyticsOverviewResponse
{
    public int TotalRequestsInWindow { get; set; }

    public int CompletedRequestsInWindow { get; set; }

    public int ActiveJobs { get; set; }

    public int SensitiveActions24h { get; set; }

    public int DeniedActions24h { get; set; }

    public IReadOnlyList<ManagementTrendPointResponse> IntakeTrend { get; set; } = Array.Empty<ManagementTrendPointResponse>();

    public IReadOnlyList<ManagementTrendPointResponse> CompletionTrend { get; set; } = Array.Empty<ManagementTrendPointResponse>();

    public IReadOnlyList<ManagementAnomalyIndicatorResponse> Anomalies { get; set; } = Array.Empty<ManagementAnomalyIndicatorResponse>();

    public IReadOnlyList<ManagementDrilldownItemResponse> ActionDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();

    public IReadOnlyList<ManagementDrilldownItemResponse> OutcomeDrilldown { get; set; } = Array.Empty<ManagementDrilldownItemResponse>();
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
