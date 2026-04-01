namespace GTEK.FSM.Backend.Application.Audit;

public sealed class AuditLogsQueryResult
{
    private AuditLogsQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedAuditLogsPage? payload)
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

    public QueriedAuditLogsPage? Payload { get; }

    public static AuditLogsQueryResult Success(QueriedAuditLogsPage payload)
    {
        return new AuditLogsQueryResult(true, "Audit logs retrieved.", null, null, payload);
    }

    public static AuditLogsQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new AuditLogsQueryResult(false, message, errorCode, statusCode, null);
    }
}
