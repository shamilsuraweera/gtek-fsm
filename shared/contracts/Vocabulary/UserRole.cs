namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Core user roles in the GTEK FSM platform.
/// Defines the primary participant types and their responsibilities across all systems.
/// </summary>
public enum UserRole
{
    /// <summary>Guest or unauthenticated user.</summary>
    Guest = 0,

    /// <summary>Customer who submits requests for service.</summary>
    Customer = 1,

    /// <summary>Field-based worker who completes jobs and requests.</summary>
    Worker = 2,

    /// <summary>Support team member who manages requests and assignments.</summary>
    Support = 3,

    /// <summary>Manager with oversight and coordination responsibilities.</summary>
    Manager = 4,

    /// <summary>Administrator with full system access and configuration rights.</summary>
    Admin = 5
}
