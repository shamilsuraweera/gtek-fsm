using GTEK.FSM.Backend.Application.Identity;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

public interface IServiceRequestLifecycleService
{
    Task<TransitionServiceRequestResult> TransitionAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? nextStatus,
        string? rowVersion,
        CancellationToken cancellationToken = default);
}

public sealed class TransitionServiceRequestResult
{
    private TransitionServiceRequestResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        TransitionedServiceRequestPayload? payload)
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

    public TransitionedServiceRequestPayload? Payload { get; }

    public static TransitionServiceRequestResult Success(TransitionedServiceRequestPayload payload)
    {
        return new TransitionServiceRequestResult(
            isSuccess: true,
            message: "Service request status transitioned.",
            errorCode: null,
            statusCode: null,
            payload: payload);
    }

    public static TransitionServiceRequestResult Failure(string message, string errorCode, int statusCode)
    {
        return new TransitionServiceRequestResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}

public sealed record TransitionedServiceRequestPayload(
    Guid RequestId,
    Guid TenantId,
    string PreviousStatus,
    string CurrentStatus,
    DateTime UpdatedAtUtc,
    string? RowVersion);
