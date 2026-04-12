using GTEK.FSM.WebPortal.Services.Security;

namespace GTEK.FSM.WebPortal.Services.Realtime;

public sealed class PortalAuthStateAccessTokenProvider : IPortalAccessTokenProvider
{
    private readonly PortalAuthState authState;

    public PortalAuthStateAccessTokenProvider(PortalAuthState authState)
    {
        this.authState = authState;
    }

    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(this.authState.GetAccessToken());
    }
}
