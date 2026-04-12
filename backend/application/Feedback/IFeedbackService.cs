using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Feedback;

public interface IFeedbackService
{
    /// <summary>
    /// Creates feedback for a completed job/request.
    /// </summary>
    Task<Guid> SubmitFeedbackAsync(
        Guid tenantId,
        Guid jobId,
        Guid serviceRequestId,
        Guid providedByUserId,
        FeedbackSource source,
        decimal? rating,
        string? comment,
        FeedbackType type,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feedback for a service request.
    /// </summary>
    Task<IReadOnlyList<FeedbackData>> GetFeedbackForServiceRequestAsync(
        Guid tenantId,
        Guid serviceRequestId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feedback for a job.
    /// </summary>
    Task<IReadOnlyList<FeedbackData>> GetFeedbackForJobAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated feedback for the tenant.
    /// </summary>
    Task<(IReadOnlyList<FeedbackData> Items, int TotalCount)> QueryFeedbackAsync(
        Guid tenantId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actionable feedback for the tenant.
    /// </summary>
    Task<IReadOnlyList<FeedbackData>> GetActionableFeedbackAsync(
        Guid tenantId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feedback statistics for the tenant.
    /// </summary>
    Task<FeedbackStatistics> GetFeedbackStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}

public class FeedbackData
{
    public required Guid Id { get; set; }

    public required Guid JobId { get; set; }

    public required Guid ServiceRequestId { get; set; }

    public required Guid ProvidedByUserId { get; set; }

    public required FeedbackSource Source { get; set; }

    public required decimal Rating { get; set; }

    public required string Comment { get; set; }

    public required FeedbackType Type { get; set; }

    public required bool IsActionable { get; set; }

    public required DateTime CreatedAtUtc { get; set; }
}

public class FeedbackStatistics
{
    public int TotalFeedbackCount { get; set; }

    public decimal AverageRating { get; set; }

    public int CustomerFeedbackCount { get; set; }

    public int WorkerFeedbackCount { get; set; }

    public decimal AverageCustomerRating { get; set; }

    public decimal AverageWorkerRating { get; set; }

    public int ActionableFeedbackCount { get; set; }

    public Dictionary<FeedbackType, int> FeedbackCountByType { get; set; } = new();
}
