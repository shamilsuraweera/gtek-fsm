namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Common;

public sealed class ConflictResponse
{
    public string ErrorCode { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string? CurrentRowVersion { get; set; }
}
