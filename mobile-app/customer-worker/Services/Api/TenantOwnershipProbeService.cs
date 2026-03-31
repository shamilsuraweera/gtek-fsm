namespace GTEK.FSM.MobileApp.Services.Api;

using System.Net.Http.Headers;
using GTEK.FSM.MobileApp.Services.Diagnostics;
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
    private readonly IMobileDiagnosticsLogger _diagnostics;

    public TenantOwnershipProbeService(
        HttpClient httpClient,
        IIdentityTokenProvider tokenProvider,
        TenantContextState tenantContextState,
        IMobileDiagnosticsLogger diagnostics)
    {
        _httpClient = httpClient;
        _tokenProvider = tokenProvider;
        _tenantContextState = tenantContextState;
        _diagnostics = diagnostics;
    }

    public async Task<bool> ProbeReadBoundaryAsync(CancellationToken cancellationToken = default)
    {
        var accessToken = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(accessToken) || !_tenantContextState.HasTenantContext)
        {
            _diagnostics.Warn("tenant.probe", "Skipped tenant ownership probe because token or tenant context is missing.");
            return false;
        }

        var tenantId = _tenantContextState.TenantId;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/tenant/{tenantId}/ownership-check/read");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Add("X-Tenant-Id", tenantId);

        _diagnostics.Info("tenant.probe", $"Dispatching tenant ownership probe for tenant '{tenantId}'.");

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _diagnostics.Warn("tenant.probe", $"Tenant probe failed with HTTP {(int)response.StatusCode} ({response.StatusCode}).");
        }
        else
        {
            _diagnostics.Info("tenant.probe", "Tenant ownership probe succeeded.");
        }

        return response.IsSuccessStatusCode;
    }
}
