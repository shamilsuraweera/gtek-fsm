namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net.Http.Headers;
using GTEK.FSM.MobileApp.Services.Identity;

public interface IAuthenticatedApiProbeService
{
    Task<bool> ProbeAuthenticatedAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthenticatedApiProbeService : IAuthenticatedApiProbeService
{
    private readonly HttpClient _httpClient;
    private readonly IIdentityTokenProvider _tokenProvider;

    public AuthenticatedApiProbeService(HttpClient httpClient, IIdentityTokenProvider tokenProvider)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
    }

    public async Task<bool> ProbeAuthenticatedAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/bootstrap/authenticated");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
