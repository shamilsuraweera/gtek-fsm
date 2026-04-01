namespace GTEK.FSM.Backend.Application.Audit;

public sealed record QueriedAuditLogsPage(
    IReadOnlyList<QueriedAuditLogItem> Items,
    int Page,
    int PageSize,
    int Total);
