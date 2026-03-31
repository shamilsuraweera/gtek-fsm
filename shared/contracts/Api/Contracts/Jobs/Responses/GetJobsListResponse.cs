using GTEK.FSM.Shared.Contracts.Api.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;

public sealed class GetJobsListResponse
{
    public IReadOnlyList<GetJobsResponse> Items { get; set; } = Array.Empty<GetJobsResponse>();

    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}
