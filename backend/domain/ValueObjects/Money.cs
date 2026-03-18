namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Monetary amount with ISO-4217 currency code.
/// </summary>
public sealed record Money
{
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));
        }

        this.Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        this.Currency = NormalizeCurrency(currency);
    }

    public decimal Amount { get; }

    public string Currency { get; }

    public static Money Zero(string currency)
    {
        return new Money(0m, currency);
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(this, other);
        return new Money(this.Amount + other.Amount, this.Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentException("Multiplication factor cannot be negative.", nameof(factor));
        }

        return new Money(this.Amount * factor, this.Currency);
    }

    private static string NormalizeCurrency(string currency)
    {
        var normalized = !string.IsNullOrWhiteSpace(currency)
            ? currency.Trim().ToUpperInvariant()
            : throw new ArgumentException("Currency is required.", nameof(currency));

        if (normalized.Length != 3)
        {
            throw new ArgumentException("Currency must be ISO-4217 alpha-3.", nameof(currency));
        }

        return normalized;
    }

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (!string.Equals(left.Currency, right.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Money operations require matching currencies.");
        }
    }
}
