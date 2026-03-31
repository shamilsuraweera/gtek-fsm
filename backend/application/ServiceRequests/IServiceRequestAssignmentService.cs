using GTEK.FSM.Backend.Application.Identity;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

public interface IServiceRequestAssignmentService
{
    Task<ServiceRequestAssignmentResult> AssignAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? workerUserId,
        CancellationToken cancellationToken = default);

    Task<ServiceRequestAssignmentResult> ReassignAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? workerUserId,
        CancellationToken cancellationToken = default);
}

public sealed class ServiceRequestAssignmentResult
{
    private ServiceRequestAssignmentResult(
        bool isSuccess,
        string message,
        string? errorCode,
        int? statusCode,
        AssignedServiceRequestPayload? payload)
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

    public AssignedServiceRequestPayload? Payload { get; }

    public static ServiceRequestAssignmentResult Success(AssignedServiceRequestPayload payload, string message)
    {
        return new ServiceRequestAssignmentResult(
            isSuccess: true,
            message: message,
            errorCode: null,
            statusCode: null,
            payload: payload);
    }

    public static ServiceRequestAssignmentResult Failure(string message, string errorCode, int statusCode)
    {
        return new ServiceRequestAssignmentResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            statusCode: statusCode,
            payload: null);
    }
}

public sealed record AssignedServiceRequestPayload(
    Guid RequestId,
    Guid TenantId,
    Guid JobId,
    Guid? PreviousWorkerUserId,
    Guid CurrentWorkerUserId,
    string AssignmentStatus,
    DateTime UpdatedAtUtc);
