namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;

public sealed class AuthSessionResponse
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAtUtc { get; set; }

    public string UserId { get; set; } = string.Empty;

    public string TenantId { get; set; } = string.Empty;

    public string TenantCode { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;
}
