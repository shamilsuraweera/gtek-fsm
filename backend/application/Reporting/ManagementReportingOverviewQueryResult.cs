namespace GTEK.FSM.Backend.Application.Reporting;

public sealed class ManagementReportingOverviewQueryResult
{
    private ManagementReportingOverviewQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedManagementAnalyticsOverview? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.StatusCode = statusCode;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public int? StatusCode { get; }

    public QueriedManagementAnalyticsOverview? Payload { get; }

    public static ManagementReportingOverviewQueryResult Success(QueriedManagementAnalyticsOverview payload)
    {
        return new ManagementReportingOverviewQueryResult(true, "Management analytics retrieved.", null, null, payload);
    }

    public static ManagementReportingOverviewQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new ManagementReportingOverviewQueryResult(false, message, errorCode, statusCode, null);
    }
}
