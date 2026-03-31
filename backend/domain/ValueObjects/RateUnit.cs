namespace GTEK.FSM.Backend.Domain.ValueObjects;

/// <summary>
/// Unit used to interpret a billing rate.
/// </summary>
public enum RateUnit
{
    /// <summary>Flat rate charged per completed job.</summary>
    PerJob = 0,

    /// <summary>Rate charged per hour of work.</summary>
    PerHour = 1,

    /// <summary>Rate charged per full day of work.</summary>
    PerDay = 2
}
