namespace GTEK.FSM.Backend.Api.Authentication;

public sealed class JwtAuthenticationOptions
{
    public const string SectionName = "Authentication:Jwt";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;

    public string SigningKey { get; set; } = string.Empty;

    public void Validate()
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
    }
}
