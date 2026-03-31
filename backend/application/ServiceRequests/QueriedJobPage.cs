namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedJobPage(
    IReadOnlyList<QueriedJobItem> Items,
    int Page,
    int PageSize,
    int Total);
