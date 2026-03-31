using GTEK.FSM.Shared.Contracts.Api.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

public sealed class GetRequestsListResponse
{
    public IReadOnlyList<GetRequestsResponse> Items { get; set; } = Array.Empty<GetRequestsResponse>();

    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}
