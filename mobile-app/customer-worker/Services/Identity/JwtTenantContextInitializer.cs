namespace GTEK.FSM.MobileApp.Services.Identity;

using System.Text;
using System.Text.Json;
using GTEK.FSM.MobileApp.State;

public interface ITenantContextInitializer
{
    bool TryInitializeFromToken();
}

public sealed class JwtTenantContextInitializer : ITenantContextInitializer
{
    private readonly IIdentityTokenProvider _tokenProvider;
    private readonly TenantContextState _tenantContextState;
    private readonly SessionContextState _sessionContextState;

    public JwtTenantContextInitializer(
        IIdentityTokenProvider tokenProvider,
        TenantContextState tenantContextState,
        SessionContextState sessionContextState)
    {
        _tokenProvider = tokenProvider;
        _tenantContextState = tenantContextState;
        _sessionContextState = sessionContextState;
    }

    public bool TryInitializeFromToken()
    {
        var token = _tokenProvider.GetAccessToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        if (!TryReadPayload(token, out var payload))
        {
            return false;
        }

        if (!payload.TryGetProperty("tenant_id", out var tenantIdProperty))
        {
            return false;
        }

        var tenantId = tenantIdProperty.GetString();
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            return false;
        }

        var userId = payload.TryGetProperty("sub", out var subProperty)
            ? subProperty.GetString() ?? string.Empty
            : string.Empty;

        var role = payload.TryGetProperty("role", out var roleProperty)
            ? roleProperty.GetString() ?? string.Empty
            : string.Empty;

        _tenantContextState.Update(tenantId, "JWT tenant context");
        _sessionContextState.Update(userId, role, isSessionActive: true);
        return true;
    }

    private static bool TryReadPayload(string jwt, out JsonElement payload)
    {
        payload = default;

        var segments = jwt.Split('.');
        if (segments.Length < 2 || string.IsNullOrWhiteSpace(segments[1]))
        {
            return false;
        }

        try
        {
            var bytes = DecodeBase64Url(segments[1]);
            using var document = JsonDocument.Parse(bytes);
            payload = document.RootElement.Clone();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static byte[] DecodeBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        var mod4 = padded.Length % 4;
        if (mod4 > 0)
        {
            padded = padded.PadRight(padded.Length + (4 - mod4), '=');
        }

        return Convert.FromBase64String(padded);
    }
}
