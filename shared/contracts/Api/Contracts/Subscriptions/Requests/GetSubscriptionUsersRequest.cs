namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

public sealed class GetSubscriptionUsersRequest
{
    public int? Page { get; set; }

    public int? PageSize { get; set; }

    public string? SearchText { get; set; }
}