namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net.Http.Headers;
using GTEK.FSM.MobileApp.Services.Diagnostics;
using GTEK.FSM.MobileApp.Services.Identity;

public interface IAuthenticatedApiProbeService
{
    Task<bool> ProbeAuthenticatedAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthenticatedApiProbeService : IAuthenticatedApiProbeService
{
    private readonly HttpClient _httpClient;
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly IMobileDiagnosticsLogger _diagnostics;

    public AuthenticatedApiProbeService(
        HttpClient httpClient,
        IIdentityTokenProvider tokenProvider,
        IMobileDiagnosticsLogger diagnostics)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _diagnostics = diagnostics;
    }

    public async Task<bool> ProbeAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _diagnostics.Warn("auth.probe", "Skipped auth probe because identity token is missing.");
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/bootstrap/authenticated");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        _diagnostics.Info("auth.probe", "Dispatching authenticated bootstrap probe request.");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _diagnostics.Warn("auth.probe", $"Auth probe failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }
        else
        {
            _diagnostics.Info("auth.probe", "Auth probe succeeded.");
        }

        return response.IsSuccessStatusCode;
    }
}
