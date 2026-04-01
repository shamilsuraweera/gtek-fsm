namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;

public sealed class WorkerProfileResponse
{
    public string? WorkerId { get; set; }

    public string? TenantId { get; set; }

    public string? WorkerCode { get; set; }

    public string? DisplayName { get; set; }

    public decimal InternalRating { get; set; }

    public string? AvailabilityStatus { get; set; }

    public bool IsActive { get; set; }

    public string[] Skills { get; set; } = [];

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
