using GTEK.FSM.Shared.Contracts.Api.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;

public sealed class GetWorkersListResponse
{
    public WorkerProfileResponse[] Items { get; set; } = [];

    public PaginationMetadata? Pagination { get; set; }
}
