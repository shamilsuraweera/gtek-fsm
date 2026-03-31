namespace GTEK.FSM.MobileApp.Services.Identity;

public sealed class EnvironmentIdentityTokenProvider : IIdentityTokenProvider
{
    public string GetAccessToken()
    {
        var token = Environment.GetEnvironmentVariable("GTEK_FSM_IDENTITY_TOKEN");
        return string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
    }
}
