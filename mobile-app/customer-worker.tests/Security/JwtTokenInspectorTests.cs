namespace GTEK.FSM.MobileApp.Tests.Security;

using System.Text;
using System.Text.Json;
using GTEK.FSM.MobileApp.Services.Security;

public sealed class JwtTokenInspectorTests
{
    [Fact]
    public void TryGetExpiryUtc_WhenValidExpClaimPresent_ReturnsTrueAndParsesUtc()
    {
        var expectedUtc = DateTimeOffset.UtcNow.AddMinutes(10);
        var token = BuildJwt(new { exp = expectedUtc.ToUnixTimeSeconds() });

        var ok = JwtTokenInspector.TryGetExpiryUtc(token, out var actualUtc);

        Assert.True(ok);
        Assert.Equal(expectedUtc.ToUnixTimeSeconds(), actualUtc.ToUnixTimeSeconds());
    }

    [Fact]
    public void TryGetExpiryUtc_WhenExpClaimMissing_ReturnsFalse()
    {
        var token = BuildJwt(new { sub = "user-1" });

        var ok = JwtTokenInspector.TryGetExpiryUtc(token, out _);

        Assert.False(ok);
    }

    [Fact]
    public void TryGetExpiryUtc_WhenJwtMalformed_ReturnsFalse()
    {
        var ok = JwtTokenInspector.TryGetExpiryUtc("not-a-jwt", out _);

        Assert.False(ok);
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
}
