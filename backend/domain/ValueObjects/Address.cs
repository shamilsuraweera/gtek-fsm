namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Canonical physical address.
/// </summary>
public sealed record Address
{
    public Address(
        string line1,
        string city,
        string stateOrProvince,
        string postalCode,
        string countryCode,
        string? line2 = null)
    {
        this.Line1 = Require(line1, nameof(line1));
        this.City = Require(city, nameof(city));
        this.StateOrProvince = Require(stateOrProvince, nameof(stateOrProvince));
        this.PostalCode = Require(postalCode, nameof(postalCode));
        this.CountryCode = NormalizeCountryCode(countryCode);
        this.Line2 = string.IsNullOrWhiteSpace(line2) ? null : line2.Trim();
    }

    public string Line1 { get; }

    public string? Line2 { get; }

    public string City { get; }

    public string StateOrProvince { get; }

    public string PostalCode { get; }

    public string CountryCode { get; }

    private static string Require(string value, string paramName)
    {
        return !string.IsNullOrWhiteSpace(value)
            ? value.Trim()
            : throw new ArgumentException("Address field is required.", paramName);
    }

    private static string NormalizeCountryCode(string countryCode)
    {
        var normalized = Require(countryCode, nameof(countryCode)).ToUpperInvariant();
        if (normalized.Length != 2)
        {
            throw new ArgumentException("Country code must be ISO-3166 alpha-2.", nameof(countryCode));
        }

        return normalized;
    }
}
