namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Availability status for workers and resources.
/// Indicates whether a worker is available to accept new work or assignments.
/// </summary>
public enum AvailabilityStatus
{
    /// <summary>Worker is available and can accept new work.</summary>
    Available = 0,

    /// <summary>Worker is currently busy and cannot accept new work.</summary>
    Busy = 1,

    /// <summary>Worker is off duty and not available.</summary>
    OffDuty = 2,

    /// <summary>Worker is on leave and unavailable for an extended period.</summary>
    OnLeave = 3
}
