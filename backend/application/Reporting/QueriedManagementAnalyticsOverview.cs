namespace GTEK.FSM.Backend.Application.Reporting;

public sealed record QueriedManagementAnalyticsOverview(
    int TotalRequestsInWindow,
    int CompletedRequestsInWindow,
    int ActiveJobs,
    int SensitiveActions24h,
    int DeniedActions24h,
    IReadOnlyList<QueriedManagementTrendPoint> IntakeTrend,
    IReadOnlyList<QueriedManagementTrendPoint> CompletionTrend,
    IReadOnlyList<QueriedManagementAnomalyIndicator> Anomalies,
    IReadOnlyList<QueriedManagementDrilldownItem> ActionDrilldown,
    IReadOnlyList<QueriedManagementDrilldownItem> OutcomeDrilldown);

public sealed record QueriedManagementTrendPoint(DateTime DateUtc, int Value);

public sealed record QueriedManagementAnomalyIndicator(string Code, string Severity, string Message);

public sealed record QueriedManagementDrilldownItem(string Key, int Count);
