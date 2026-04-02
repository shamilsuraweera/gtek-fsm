namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

public sealed class UpdateWorkerProfileRequest
{
    public string? WorkerCode { get; set; }

    public string? DisplayName { get; set; }

    public decimal? InternalRating { get; set; }

    public string[]? Skills { get; set; }

    public decimal? BaseLatitude { get; set; }

    public decimal? BaseLongitude { get; set; }

    public bool? IsActive { get; set; }

    public string? AvailabilityStatus { get; set; }
}
