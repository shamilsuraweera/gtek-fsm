namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Normalized billing rate.
/// </summary>
public sealed record Rate
{
    public Rate(Money amount, RateUnit unit)
    {
        this.Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        this.Unit = unit;
    }

    public Money Amount { get; }

    public RateUnit Unit { get; }

    public Money CalculateCharge(decimal quantity)
    {
        if (quantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative.", nameof(quantity));
        }

        return this.Amount.Multiply(quantity);
    }
}
