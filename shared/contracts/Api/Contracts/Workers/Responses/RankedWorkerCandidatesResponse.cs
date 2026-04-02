namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;

public sealed class RankedWorkerCandidateItem
{
    public string? WorkerId { get; set; }

    public string? WorkerCode { get; set; }

    public string? DisplayName { get; set; }

    public decimal InternalRating { get; set; }

    public string[] Skills { get; set; } = [];

    public int ActiveJobCount { get; set; }

    public decimal TotalScore { get; set; }

    public decimal SkillScore { get; set; }

    public decimal LoadScore { get; set; }

    public decimal RatingScore { get; set; }
}

public sealed class RankedWorkerCandidatesResponse
{
    public RankedWorkerCandidateItem[] Candidates { get; set; } = [];

    public int TotalEvaluated { get; set; }

    public decimal SkillWeight { get; set; }

    public decimal LoadWeight { get; set; }

    public decimal RatingWeight { get; set; }
}
