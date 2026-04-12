namespace GTEK.FSM.Backend.Api.Authentication;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public void Validate(bool allowPlaceholderSecrets = true)
    {
        if (string.IsNullOrWhiteSpace(this.Issuer))
        {
            throw new InvalidOperationException($"{SectionName}:Issuer is required.");
        }

        if (string.IsNullOrWhiteSpace(this.Audience))
        {
            throw new InvalidOperationException($"{SectionName}:Audience is required.");
        }

        if (string.IsNullOrWhiteSpace(this.SigningKey))
        {
            throw new InvalidOperationException($"{SectionName}:SigningKey is required.");
        }

        if (this.SigningKey.Trim().Length < 32)
        {
            throw new InvalidOperationException($"{SectionName}:SigningKey must be at least 32 characters.");
        }

        if (!allowPlaceholderSecrets && LooksLikePlaceholderSecret(this.SigningKey))
        {
            throw new InvalidOperationException($"{SectionName}:SigningKey cannot use placeholder/dev values outside Development or Local environments.");
        }
    }

    public static bool LooksLikePlaceholderSecret(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = value.Trim().ToLowerInvariant();
        return normalized.Contains("change_me", StringComparison.Ordinal)
            || normalized.Contains("placeholder", StringComparison.Ordinal)
            || normalized.Contains("local-only", StringComparison.Ordinal)
            || normalized.Contains("example", StringComparison.Ordinal)
            || normalized.Contains("sample", StringComparison.Ordinal)
            || normalized.Contains("your-secret", StringComparison.Ordinal)
            || normalized.Contains("replace-me", StringComparison.Ordinal);
    }
}
