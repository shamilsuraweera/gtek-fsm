namespace GTEK.FSM.Shared.Contracts.Results;

public sealed class ApiErrorResponse : ApiResponse<object>
{
    public static ApiErrorResponse Create(string message, string? errorCode = null, string? traceId = null)
    {
        return new ApiErrorResponse
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            TraceId = traceId,
            Data = null
        };
    }
}
