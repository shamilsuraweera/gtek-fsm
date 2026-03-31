namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;

public sealed class GetSubscriptionUserResponse
{
    public string UserId { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string ExternalIdentity { get; set; } = string.Empty;

    public bool IsWithinCurrentPlanLimit { get; set; }
}