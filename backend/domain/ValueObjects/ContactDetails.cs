namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Contact details value object.
/// </summary>
public sealed record ContactDetails
{
    public ContactDetails(string email, string phoneNumber)
    {
        this.Email = ValidateEmail(email);
        this.PhoneNumber = ValidatePhone(phoneNumber);
    }

    public string Email { get; }

    public string PhoneNumber { get; }

    private static string ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        var normalized = email.Trim();
        if (!normalized.Contains('@'))
        {
            throw new ArgumentException("Email format is invalid.", nameof(email));
        }

        return normalized;
    }

    private static string ValidatePhone(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number is required.", nameof(phoneNumber));
        }

        var normalized = phoneNumber.Trim();
        if (normalized.Length < 7)
        {
            throw new ArgumentException("Phone number is too short.", nameof(phoneNumber));
        }

        return normalized;
    }
}
