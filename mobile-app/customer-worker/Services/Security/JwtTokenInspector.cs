namespace GTEK.FSM.MobileApp.Services.Security;

using System.Text.Json;

public static class JwtTokenInspector
{
    public static bool TryGetExpiryUtc(string jwt, out DateTimeOffset expiryUtc)
    {
        expiryUtc = default;

        if (!TryReadPayload(jwt, out var payload))
        {
            return false;
        }

        if (!payload.TryGetProperty("exp", out var expiryProperty))
        {
            return false;
        }

        if (!TryReadUnixSeconds(expiryProperty, out var unixSeconds))
        {
            return false;
        }

        expiryUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
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

    private static bool TryReadUnixSeconds(JsonElement element, out long unixSeconds)
    {
        unixSeconds = default;

        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt64(out unixSeconds);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return long.TryParse(element.GetString(), out unixSeconds);
        }

        return false;
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