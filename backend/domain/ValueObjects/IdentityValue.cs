namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Strongly typed external identity token from an identity provider.
/// </summary>
public sealed record IdentityValue
{
    public IdentityValue(string provider, string subject)
    {
        this.Provider = !string.IsNullOrWhiteSpace(provider)
            ? provider.Trim().ToLowerInvariant()
            : throw new ArgumentException("Identity provider is required.", nameof(provider));

        this.Subject = !string.IsNullOrWhiteSpace(subject)
            ? subject.Trim()
            : throw new ArgumentException("Identity subject is required.", nameof(subject));
    }

    public string Provider { get; }

    public string Subject { get; }

    public override string ToString()
    {
        return $"{this.Provider}:{this.Subject}";
    }
}
