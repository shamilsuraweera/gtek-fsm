namespace GTEK.FSM.Backend.Application.Decisioning;

/// <summary>
/// Weighting configuration for the worker matching scorer.
/// All weights must be non-negative and sum to 1.0.
/// </summary>
public sealed record WorkerMatchingWeights
{
    public static readonly WorkerMatchingWeights Default = new(0.4m, 0.25m, 0.15m, 0.2m);

    public WorkerMatchingWeights(decimal skillWeight, decimal loadWeight, decimal ratingWeight, decimal distanceWeight)
    {
        if (skillWeight < 0m || loadWeight < 0m || ratingWeight < 0m || distanceWeight < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(skillWeight), "Matching weights must be non-negative.");
        }

        var total = skillWeight + loadWeight + ratingWeight + distanceWeight;
        if (Math.Abs(total - 1.0m) > 0.0001m)
        {
            throw new ArgumentException($"Matching weights must sum to 1.0, but sum was {total}.");
        }

        this.SkillWeight = skillWeight;
        this.LoadWeight = loadWeight;
        this.RatingWeight = ratingWeight;
        this.DistanceWeight = distanceWeight;
    }

    public decimal SkillWeight { get; }

    public decimal LoadWeight { get; }

    public decimal RatingWeight { get; }

    public decimal DistanceWeight { get; }
}
