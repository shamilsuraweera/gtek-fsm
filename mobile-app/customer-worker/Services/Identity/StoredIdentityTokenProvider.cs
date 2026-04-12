namespace GTEK.FSM.MobileApp.Services.Identity;

public sealed class StoredIdentityTokenProvider : IIdentityTokenProvider
{
    private const string StorageKey = "gtek_fsm_identity_token";
    private string? accessToken;

    public string GetAccessToken()
    {
        if (!string.IsNullOrWhiteSpace(this.accessToken))
        {
            return this.accessToken;
        }

        var storedToken = Preferences.Default.Get(StorageKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(storedToken))
        {
            this.accessToken = storedToken.Trim();
            return this.accessToken;
        }

        var envToken = Environment.GetEnvironmentVariable("GTEK_FSM_IDENTITY_TOKEN");
        this.accessToken = string.IsNullOrWhiteSpace(envToken) ? string.Empty : envToken.Trim();
        return this.accessToken;
    }

    public void SetAccessToken(string token)
    {
        this.accessToken = string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();

        if (string.IsNullOrWhiteSpace(this.accessToken))
        {
            Preferences.Default.Remove(StorageKey);
            return;
        }

        Preferences.Default.Set(StorageKey, this.accessToken);
        Environment.SetEnvironmentVariable("GTEK_FSM_IDENTITY_TOKEN", this.accessToken);
    }

    public void ClearAccessToken()
    {
        this.accessToken = string.Empty;
        Preferences.Default.Remove(StorageKey);
        Environment.SetEnvironmentVariable("GTEK_FSM_IDENTITY_TOKEN", string.Empty);
    }
}
