namespace GTEK.FSM.Shared.Contracts.Vocabulary;

/// <summary>
/// Subscription service tiers defining feature access and operational limits.
/// Used to control platform capabilities and quota boundaries.
/// </summary>
public enum SubscriptionTier
{
    /// <summary>Free tier with limited features and capacity.</summary>
    Free = 0,

    /// <summary>Professional tier with standard features and moderate capacity.</summary>
    Professional = 1,

    /// <summary>Enterprise tier with full features and unlimited capacity.</summary>
    Enterprise = 2
}
