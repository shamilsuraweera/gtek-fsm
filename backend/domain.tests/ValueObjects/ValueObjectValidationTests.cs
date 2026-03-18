using GTEK.FSM.Backend.Domain.ValueObjects;
using Xunit;

namespace GTEK.FSM.Backend.Domain.Tests.ValueObjects;

public class ValueObjectValidationTests
{
    [Fact]
    public void IdentityValue_WithProviderAndSubject_NormalizesProvider()
    {
        var identity = new IdentityValue("AzureAD", "subject-123");

        Assert.Equal("azuread", identity.Provider);
        Assert.Equal("subject-123", identity.Subject);
        Assert.Equal("azuread:subject-123", identity.ToString());
    }

    [Fact]
    public void ContactDetails_InvalidEmail_Throws()
    {
        Assert.Throws<ArgumentException>(() => new ContactDetails("invalid", "+9411222333"));
    }

    [Fact]
    public void Address_InvalidCountryCode_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Address("No 1", "Colombo", "Western", "00100", "LKA"));
    }

    [Fact]
    public void Money_Add_WithDifferentCurrencies_Throws()
    {
        var usd = new Money(10m, "USD");
        var eur = new Money(5m, "EUR");

        Assert.Throws<InvalidOperationException>(() => usd.Add(eur));
    }

    [Fact]
    public void Rate_CalculateCharge_ReturnsExpectedMoney()
    {
        var rate = new Rate(new Money(100m, "USD"), RateUnit.PerHour);

        var total = rate.CalculateCharge(2.5m);

        Assert.Equal(250m, total.Amount);
        Assert.Equal("USD", total.Currency);
    }

    [Fact]
    public void SchedulingWindow_EndBeforeStart_Throws()
    {
        var start = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var end = start.AddMinutes(-5);

        Assert.Throws<ArgumentException>(() => new SchedulingWindow(start, end));
    }

    [Fact]
    public void SchedulingWindow_Overlaps_WhenIntervalsIntersect()
    {
        var aStart = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        var aEnd = aStart.AddHours(2);
        var bStart = aStart.AddHours(1);
        var bEnd = bStart.AddHours(2);

        var first = new SchedulingWindow(aStart, aEnd);
        var second = new SchedulingWindow(bStart, bEnd);

        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }
}
