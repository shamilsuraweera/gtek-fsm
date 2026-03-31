namespace GTEK.FSM.Backend.Application.Subscriptions;

public sealed record QueriedSubscriptionUsersPage(
    IReadOnlyList<QueriedSubscriptionUserItem> Items,
    int Page,
    int PageSize,
    int Total);