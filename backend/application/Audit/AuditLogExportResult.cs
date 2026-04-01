namespace GTEK.FSM.Backend.Application.Audit;

public sealed class AuditLogExportResult
{
    private AuditLogExportResult(bool isSuccess, string message, string? errorCode, int? statusCode, IReadOnlyList<QueriedAuditLogItem>? payload)
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

    public IReadOnlyList<QueriedAuditLogItem>? Payload { get; }

    public static AuditLogExportResult Success(IReadOnlyList<QueriedAuditLogItem> payload)
    {
        return new AuditLogExportResult(true, "Audit logs exported.", null, null, payload);
    }

    public static AuditLogExportResult Failure(string message, string errorCode, int statusCode)
    {
        return new AuditLogExportResult(false, message, errorCode, statusCode, null);
    }
}
