namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

/// <summary>
/// Request payload for reassigning a service request to a different worker.
/// </summary>
public sealed class ReassignServiceRequestRequest
{
    public string? WorkerUserId { get; set; }

    public string? RowVersion { get; set; }
}
