namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

/// <summary>
/// Request payload for assigning a worker to a service request.
/// </summary>
public sealed class AssignServiceRequestRequest
{
    public string? WorkerUserId { get; set; }
}
