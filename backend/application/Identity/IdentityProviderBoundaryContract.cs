namespace GTEK.FSM.Backend.Application.Identity;

/// <summary>
/// Identity-provider boundary contract used at the application edge.
/// </summary>
public sealed record IdentityProviderBoundaryContract
{
    public IdentityProviderBoundaryContract(string provider, string subject, string issuer)
    {
        this.Provider = !string.IsNullOrWhiteSpace(provider)
            ? provider.Trim().ToLowerInvariant()
            : throw new ArgumentException("Identity provider is required.", nameof(provider));

        this.Subject = !string.IsNullOrWhiteSpace(subject)
            ? subject.Trim()
            : throw new ArgumentException("Identity subject is required.", nameof(subject));

        this.Issuer = !string.IsNullOrWhiteSpace(issuer)
            ? issuer.Trim()
            : throw new ArgumentException("Identity issuer is required.", nameof(issuer));
    }

    public string Provider { get; }

    public string Subject { get; }

    public string Issuer { get; }
}
