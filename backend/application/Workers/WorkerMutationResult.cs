namespace GTEK.FSM.Backend.Application.Workers;

public sealed class WorkerMutationResult
{
    private WorkerMutationResult(bool isSuccess, string message, string? errorCode, int? statusCode, QueriedWorkerProfileItem? payload)
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

    public QueriedWorkerProfileItem? Payload { get; }

    public static WorkerMutationResult Success(QueriedWorkerProfileItem payload, string message)
    {
        return new WorkerMutationResult(true, message, null, null, payload);
    }

    public static WorkerMutationResult Failure(string message, string errorCode, int statusCode)
    {
        return new WorkerMutationResult(false, message, errorCode, statusCode, null);
    }
}
