namespace GTEK.FSM.Backend.Application.Workers;

public sealed class WorkerProfilesQueryResult
{
    private WorkerProfilesQueryResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedWorkerProfilesPage? payload)
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

    public QueriedWorkerProfilesPage? Payload { get; }

    public static WorkerProfilesQueryResult Success(QueriedWorkerProfilesPage payload)
    {
        return new WorkerProfilesQueryResult(true, "Worker profiles retrieved.", null, null, payload);
    }

    public static WorkerProfilesQueryResult Failure(string message, string errorCode, int statusCode)
    {
        return new WorkerProfilesQueryResult(false, message, errorCode, statusCode, null);
    }
}
