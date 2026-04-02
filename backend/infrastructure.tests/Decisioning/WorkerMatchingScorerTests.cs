using GTEK.FSM.Backend.Application.Decisioning;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Decisioning;

public sealed class WorkerMatchingScorerTests
{
    // ── WorkerMatchingWeights validation ───────────────────────────────────

    [Fact]
    public void Weights_DefaultValues_SumToOne()
    {
        var w = WorkerMatchingWeights.Default;
        Assert.Equal(1.0m, w.SkillWeight + w.LoadWeight + w.RatingWeight);
    }

    [Fact]
    public void Weights_CustomValid_Accepted()
    {
        var w = new WorkerMatchingWeights(0.6m, 0.2m, 0.2m);
        Assert.Equal(0.6m, w.SkillWeight);
        Assert.Equal(0.2m, w.LoadWeight);
        Assert.Equal(0.2m, w.RatingWeight);
    }

    [Fact]
    public void Weights_DoNotSumToOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => new WorkerMatchingWeights(0.4m, 0.3m, 0.2m));
    }

    [Fact]
    public void Weights_NegativeValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WorkerMatchingWeights(-0.1m, 0.6m, 0.5m));
    }

    // ── SkillScore ──────────────────────────────────────────────────────────

    [Fact]
    public void SkillScore_NoRequiredSkills_ReturnsOne()
    {
        var score = WorkerMatchingScorer.ComputeSkillScore(
            requiredSkills: [],
            workerSkills: ["hvac", "plumbing"]);

        Assert.Equal(1.0m, score);
    }

    [Fact]
    public void SkillScore_AllSkillsMatched_ReturnsOne()
    {
        var score = WorkerMatchingScorer.ComputeSkillScore(
            requiredSkills: ["hvac", "plumbing"],
            workerSkills: ["hvac", "plumbing", "electrical"]);

        Assert.Equal(1.0m, score);
    }

    [Fact]
    public void SkillScore_PartialMatch_ReturnsCorrectFraction()
    {
        var score = WorkerMatchingScorer.ComputeSkillScore(
            requiredSkills: ["hvac", "plumbing"],
            workerSkills: ["hvac"]);

        Assert.Equal(0.5m, score);
    }

    [Fact]
    public void SkillScore_NoMatch_ReturnsZero()
    {
        var score = WorkerMatchingScorer.ComputeSkillScore(
            requiredSkills: ["electrical"],
            workerSkills: ["hvac"]);

        Assert.Equal(0.0m, score);
    }

    [Fact]
    public void SkillScore_CaseInsensitiveMatch_Matches()
    {
        var score = WorkerMatchingScorer.ComputeSkillScore(
            requiredSkills: ["HVAC"],
            workerSkills: ["hvac"]);

        Assert.Equal(1.0m, score);
    }

    // ── LoadScore ───────────────────────────────────────────────────────────

    [Fact]
    public void LoadScore_ZeroActiveJobs_ReturnsOne()
    {
        var score = WorkerMatchingScorer.ComputeLoadScore(0);
        Assert.Equal(1.0m, score);
    }

    [Fact]
    public void LoadScore_OneActiveJob_ReturnsHalf()
    {
        var score = WorkerMatchingScorer.ComputeLoadScore(1);
        Assert.Equal(0.5m, score);
    }

    [Fact]
    public void LoadScore_ThreeActiveJobs_ReturnsQuarter()
    {
        var score = WorkerMatchingScorer.ComputeLoadScore(3);
        Assert.Equal(0.25m, score);
    }

    [Fact]
    public void LoadScore_NegativeCount_TreatedAsZero()
    {
        var score = WorkerMatchingScorer.ComputeLoadScore(-5);
        Assert.Equal(1.0m, score);
    }

    // ── RatingScore ─────────────────────────────────────────────────────────

    [Fact]
    public void RatingScore_MaxRating_ReturnsOne()
    {
        var score = WorkerMatchingScorer.ComputeRatingScore(5.0m);
        Assert.Equal(1.0m, score);
    }

    [Fact]
    public void RatingScore_Zero_ReturnsZero()
    {
        var score = WorkerMatchingScorer.ComputeRatingScore(0m);
        Assert.Equal(0.0m, score);
    }

    [Fact]
    public void RatingScore_MidRating_ReturnsHalf()
    {
        var score = WorkerMatchingScorer.ComputeRatingScore(2.5m);
        Assert.Equal(0.5m, score);
    }

    [Fact]
    public void RatingScore_AboveMax_ClampedToOne()
    {
        var score = WorkerMatchingScorer.ComputeRatingScore(10m);
        Assert.Equal(1.0m, score);
    }

    // ── ComputeScore (total weighted) ───────────────────────────────────────

    [Fact]
    public void ComputeScore_PerfectWorker_ReturnsOne()
    {
        var score = WorkerMatchingScorer.ComputeScore(
            requiredSkills: ["hvac"],
            workerSkills: ["hvac"],
            activeJobCount: 0,
            internalRating: 5.0m,
            weights: WorkerMatchingWeights.Default);

        Assert.Equal(1.0m, score);
    }

    [Fact]
    public void ComputeScore_WeightsAppliedCorrectly()
    {
        // skill=1.0, load=0.5 (1 job), rating=0.8 (4.0/5.0), weights 0.5/0.3/0.2
        var expected = (0.5m * 1.0m) + (0.3m * 0.5m) + (0.2m * 0.8m);
        var score = WorkerMatchingScorer.ComputeScore(
            requiredSkills: ["hvac"],
            workerSkills: ["hvac"],
            activeJobCount: 1,
            internalRating: 4.0m,
            weights: WorkerMatchingWeights.Default);

        Assert.Equal(expected, score);
    }

    [Fact]
    public void ComputeScore_HighLoadLowRatingPartialSkill_LowerThanPerfect()
    {
        var perfect = WorkerMatchingScorer.ComputeScore(
            requiredSkills: ["hvac"],
            workerSkills: ["hvac"],
            activeJobCount: 0,
            internalRating: 5.0m,
            weights: WorkerMatchingWeights.Default);

        var imperfect = WorkerMatchingScorer.ComputeScore(
            requiredSkills: ["hvac", "plumbing"],
            workerSkills: ["hvac"],
            activeJobCount: 4,
            internalRating: 2.0m,
            weights: WorkerMatchingWeights.Default);

        Assert.True(imperfect < perfect);
    }

    [Fact]
    public void ComputeScore_TieBrakeByWorkerCodeIsDeterministic()
    {
        // Two identical score inputs produce the same score value (tie-break is in the service, not scorer).
        var s1 = WorkerMatchingScorer.ComputeScore(["hvac"], ["hvac"], 0, 5.0m, WorkerMatchingWeights.Default);
        var s2 = WorkerMatchingScorer.ComputeScore(["hvac"], ["hvac"], 0, 5.0m, WorkerMatchingWeights.Default);
        Assert.Equal(s1, s2);
    }
}
