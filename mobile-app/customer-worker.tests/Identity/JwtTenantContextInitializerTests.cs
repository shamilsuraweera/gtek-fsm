namespace GTEK.FSM.MobileApp.Tests.Identity;

using System.Text;
using System.Text.Json;
using GTEK.FSM.MobileApp.Services.Identity;
using GTEK.FSM.MobileApp.State;

public sealed class JwtTenantContextInitializerTests
{
    [Fact]
    public void TryInitializeFromToken_WhenTokenContainsTenantAndRole_SetsSessionAndTenantContext()
    {
        var token = BuildJwt(new
        {
            sub = "user-100",
            tenant_id = "tenant-abc",
            role = "customer",
            exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds(),
        });

        var tokenProvider = new StubTokenProvider(token);
        var tenantState = new TenantContextState();
        var sessionState = new SessionContextState();
        var sut = new JwtTenantContextInitializer(tokenProvider, tenantState, sessionState);

        var initialized = sut.TryInitializeFromToken();

        Assert.True(initialized);
        Assert.True(tenantState.HasTenantContext);
        Assert.Equal("tenant-abc", tenantState.TenantId);
        Assert.Equal("user-100", sessionState.UserId);
        Assert.Equal("customer", sessionState.Role);
        Assert.True(sessionState.IsSessionActive);
    }

    [Fact]
    public void TryInitializeFromToken_WhenRolesArrayClaimPresent_StoresCommaSeparatedRoles()
    {
        var token = BuildJwt(new
        {
            sub = "worker-101",
            tenant_id = "tenant-xyz",
            roles = new[] { "worker", "support" },
            exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds(),
        });

        var tenantState = new TenantContextState();
        var sessionState = new SessionContextState();
        var sut = new JwtTenantContextInitializer(new StubTokenProvider(token), tenantState, sessionState);

        var initialized = sut.TryInitializeFromToken();

        Assert.True(initialized);
        Assert.Equal("worker,support", sessionState.Role);
    }

    [Fact]
    public void TryInitializeFromToken_WhenTenantClaimMissing_ReturnsFalseAndKeepsStateInactive()
    {
        var token = BuildJwt(new
        {
            sub = "user-102",
            role = "customer",
            exp = DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds(),
        });

        var tenantState = new TenantContextState();
        var sessionState = new SessionContextState();
        var sut = new JwtTenantContextInitializer(new StubTokenProvider(token), tenantState, sessionState);

        var initialized = sut.TryInitializeFromToken();

        Assert.False(initialized);
        Assert.False(tenantState.HasTenantContext);
        Assert.False(sessionState.IsSessionActive);
        Assert.Equal(string.Empty, sessionState.Role);
    }

    private static string BuildJwt(object payload)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "none", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(payload);

        return $"{Base64UrlEncode(headerJson)}.{Base64UrlEncode(payloadJson)}.sig";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class StubTokenProvider : IIdentityTokenProvider
    {
        private readonly string _token;

        public StubTokenProvider(string token)
        {
            _token = token;
        }

        public string GetAccessToken()
        {
            return _token;
        }
    }
}
