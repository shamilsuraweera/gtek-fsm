using GTEK.FSM.Shared.Contracts.Api.Responses;

namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Responses;

public sealed class GetAuditLogsListResponse
{
    public IReadOnlyList<GetAuditLogResponse> Items { get; set; } = Array.Empty<GetAuditLogResponse>();

    public PaginationMetadata Pagination { get; set; } = new PaginationMetadata();
}
