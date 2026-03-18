namespace GTEK.FSM.Backend.Domain.Rules;

/// <summary>
/// Shared argument guard helpers for domain invariants.
/// </summary>
public static class DomainGuards
{
    public static Guid RequiredId(Guid value, string paramName, string message)
    {
        return value != Guid.Empty ? value : throw new ArgumentException(message, paramName);
    }

    public static string RequiredText(string? value, string paramName, string message)
    {
        return !string.IsNullOrWhiteSpace(value) ? value.Trim() : throw new ArgumentException(message, paramName);
    }

    public static string RequiredText(string? value, string paramName, string message, int maxLength)
    {
        var normalized = RequiredText(value, paramName, message);
        if (normalized.Length > maxLength)
        {
            throw new ArgumentException($"{paramName} cannot exceed {maxLength} characters.", paramName);
        }

        return normalized;
    }
}
