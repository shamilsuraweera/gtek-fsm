namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

public sealed class LoginRequest
{
    public string? Email { get; set; }

    public string? Password { get; set; }
}
