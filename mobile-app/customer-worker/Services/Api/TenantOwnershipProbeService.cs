namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net.Http.Headers;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.State;

public interface ITenantOwnershipProbeService
{
    Task<bool> ProbeReadBoundaryAsync(CancellationToken cancellationToken = default);
}

public sealed class TenantOwnershipProbeService : ITenantOwnershipProbeService
{
    private readonly HttpClient _httpClient;
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly TenantContextState _tenantContextState;

    public TenantOwnershipProbeService(
        HttpClient httpClient,
        IIdentityTokenProvider tokenProvider,
        TenantContextState tenantContextState)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _tenantContextState = tenantContextState;
    }

    public async Task<bool> ProbeReadBoundaryAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken) || !_tenantContextState.HasTenantContext)
        {
            return false;
        }

        var tenantId = _tenantContextState.TenantId;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tenant/{tenantId}/ownership-check/read");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        return response.IsSuccessStatusCode;
    }
}
