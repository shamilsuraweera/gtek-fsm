namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

public sealed class RegisterLocalUserRequest
{
    public string? DisplayName { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? TenantCode { get; set; }
}
