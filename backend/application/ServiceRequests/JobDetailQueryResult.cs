namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents the query operation outcome for job detail retrieval.
/// </summary>
public sealed class JobDetailQueryResult
{
    private JobDetailQueryResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        QueriedJobDetail? payload)
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

    public QueriedJobDetail? Payload { get; }

    public static JobDetailQueryResult Success(QueriedJobDetail payload)
    {
        return new JobDetailQueryResult(
            isSuccess: true,
            message: "Job detail retrieved.",
            errorCode: null,
            statusCode: null,
            payload: payload);
    }

    public static JobDetailQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new JobDetailQueryResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}