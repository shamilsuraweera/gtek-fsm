namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;

public sealed class GetManagementAnalyticsOverviewRequest
{
    public int? WindowDays { get; set; }

    public int? TrendBuckets { get; set; }
}
