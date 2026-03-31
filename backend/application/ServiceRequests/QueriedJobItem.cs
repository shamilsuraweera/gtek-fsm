namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed record QueriedJobItem(
    Guid JobId,
    string Title,
    string Status,
    Guid RequestId,
    Guid? AssignedTo,
    DateTime AssignedUtc);
