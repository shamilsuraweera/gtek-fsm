namespace GTEK.FSM.Backend.Application.Workers;

public sealed record QueriedWorkerProfilesPage(
    IReadOnlyList<QueriedWorkerProfileItem> Items,
    int Page,
    int PageSize,
    int Total);
