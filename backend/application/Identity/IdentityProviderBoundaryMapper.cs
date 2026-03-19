using GTEK.FSM.Backend.Domain.ValueObjects;

namespace GTEK.FSM.Backend.Application.Identity;

public static class IdentityProviderBoundaryMapper
{
    public const string LegacyProvider = "legacy";

    /// <summary>
    /// Maps boundary contract fields to the existing domain identity value object.
    /// </summary>
    public static IdentityValue ToIdentityValue(IdentityProviderBoundaryContract contract)
    {
        ArgumentNullException.ThrowIfNull(contract);
        return new IdentityValue(contract.Provider, contract.Subject);
    }

    /// <summary>
    /// Maps boundary contract to User.ExternalIdentity canonical storage value.
    /// </summary>
    public static string ToExternalIdentity(IdentityProviderBoundaryContract contract)
    {
        return ToIdentityValue(contract).ToString();
    }

    /// <summary>
    /// Maps existing User.ExternalIdentity storage to boundary fields using caller-provided issuer context.
    /// Legacy non-canonical values are interpreted as subject-only with provider set to "legacy".
    /// </summary>
    public static IdentityProviderBoundaryContract FromExternalIdentity(string externalIdentity, string issuer)
    {
        if (string.IsNullOrWhiteSpace(externalIdentity))
        {
            throw new ArgumentException("External identity is required.", nameof(externalIdentity));
        }

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new ArgumentException("Identity issuer is required.", nameof(issuer));
        }

        var trimmedIdentity = externalIdentity.Trim();
        var separatorIndex = trimmedIdentity.IndexOf(':');

        if (separatorIndex > 0 && separatorIndex < (trimmedIdentity.Length - 1))
        {
            var provider = trimmedIdentity[..separatorIndex];
            var subject = trimmedIdentity[(separatorIndex + 1)..];
            return new IdentityProviderBoundaryContract(provider, subject, issuer);
        }

        return new IdentityProviderBoundaryContract(LegacyProvider, trimmedIdentity, issuer);
    }
}
