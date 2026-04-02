namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

public sealed class GetWorkerCandidatesRequest
{
    /// <summary>Comma-separated required skill tags, e.g. "hvac,plumbing".</summary>
    public string? Skills { get; set; }

    /// <summary>Maximum number of ranked candidates to return (1–50, default 10).</summary>
    public int? TopN { get; set; }

    /// <summary>Weight for skill compatibility score (0–1). Defaults to 0.5.</summary>
    public decimal? SkillWeight { get; set; }

    /// <summary>Weight for load score (0–1). Defaults to 0.3.</summary>
    public decimal? LoadWeight { get; set; }

    /// <summary>Weight for internal rating score (0–1). Defaults to 0.2.</summary>
    public decimal? RatingWeight { get; set; }
}
