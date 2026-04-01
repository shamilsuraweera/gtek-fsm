namespace GTEK.FSM.WebPortal.Services.Realtime;

public interface IPortalAccessTokenProvider
{
    ValueTask<string?> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}