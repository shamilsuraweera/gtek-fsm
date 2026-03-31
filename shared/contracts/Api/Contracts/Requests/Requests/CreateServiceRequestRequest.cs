namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

/// <summary>
/// Request payload for creating a tenant-scoped service request.
/// </summary>
public sealed class CreateServiceRequestRequest
{
    public string? Title { get; set; }
}
