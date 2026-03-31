namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Represents the query operation outcome for service request detail retrieval.
/// </summary>
public sealed class ServiceRequestDetailQueryResult
{
    private ServiceRequestDetailQueryResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        QueriedServiceRequestDetail? payload)
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

    public QueriedServiceRequestDetail? Payload { get; }

    public static ServiceRequestDetailQueryResult Success(QueriedServiceRequestDetail payload)
    {
        return new ServiceRequestDetailQueryResult(
            isSuccess: true,
            message: "Service request detail retrieved.",
            errorCode: null,
            statusCode: null,
            payload: payload);
    }

    public static ServiceRequestDetailQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new ServiceRequestDetailQueryResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}