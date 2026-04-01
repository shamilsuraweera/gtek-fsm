namespace GTEK.FSM.WebPortal.Services.Realtime;

public sealed class NullPortalAccessTokenProvider : IPortalAccessTokenProvider
{
    public ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult<string?>(null);
    }
}