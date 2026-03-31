using GTEK.FSM.Backend.Application.Identity;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

public interface IServiceRequestCreationService
{
    Task<CreateServiceRequestResult> CreateAsync(AuthenticatedPrincipal principal, string? title, CancellationToken cancellationToken = default);
}

public sealed class CreateServiceRequestResult
{
    private CreateServiceRequestResult(bool isSuccess, string message, string? errorCode, CreatedServiceRequestPayload? payload)
    {
        this.IsSuccess = isSuccess;
        this.Message = message;
        this.ErrorCode = errorCode;
        this.Payload = payload;
    }

    public bool IsSuccess { get; }

    public string Message { get; }

    public string? ErrorCode { get; }

    public CreatedServiceRequestPayload? Payload { get; }

    public static CreateServiceRequestResult Success(CreatedServiceRequestPayload payload)
    {
        return new CreateServiceRequestResult(
            isSuccess: true,
            message: "Service request created.",
            errorCode: null,
            payload: payload);
    }

    public static CreateServiceRequestResult ValidationFailure(string message, string errorCode)
    {
        return new CreateServiceRequestResult(
            isSuccess: false,
            message: message,
            errorCode: errorCode,
            payload: null);
    }
}

public sealed record CreatedServiceRequestPayload(
    Guid RequestId,
    Guid TenantId,
    Guid CustomerUserId,
    string Title,
    string Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
