using GTEK.FSM.Shared.Contracts.Api.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;

public sealed class GetSubscriptionUsersListResponse
{
    public IReadOnlyList<GetSubscriptionUserResponse> Items { get; set; } = Array.Empty<GetSubscriptionUserResponse>();

    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}