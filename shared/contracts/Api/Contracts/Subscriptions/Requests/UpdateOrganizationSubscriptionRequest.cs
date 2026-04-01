namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

public sealed class UpdateOrganizationSubscriptionRequest
{
    public string? PlanCode { get; set; }

    public int? UserLimit { get; set; }

    public string? RowVersion { get; set; }
}