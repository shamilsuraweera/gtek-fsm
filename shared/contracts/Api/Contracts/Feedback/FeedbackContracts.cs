namespace GTEK.FSM.Shared.Contracts.Api.Contracts.Feedback;

/// <summary>
/// Request to submit feedback for a completed job/request.
/// </summary>
public class SubmitFeedbackRequest
{
    /// <summary>
    /// The job ID being reviewed.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The service request ID being reviewed.
    /// </summary>
    public Guid ServiceRequestId { get; set; }

    /// <summary>
    /// Numeric rating from 1-5, or 0 if not rated.
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>
    /// Free-form feedback comment (max 1000 chars).
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Category of feedback: 0=ServiceQuality, 1=WorkerBehavior, 2=ResponseTimeliness, 3=Communication, 4=TechnicalCompetence, 5=Other.
    /// </summary>
    public int Type { get; set; }
}

/// <summary>
/// Response containing submitted feedback details.
/// </summary>
public class FeedbackResponse
{
    /// <summary>
    /// The unique identifier for the feedback record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The job ID.
    /// </summary>
    public Guid JobId { get; set; }

    /// <summary>
    /// The service request ID.
    /// </summary>
    public Guid ServiceRequestId { get; set; }

    /// <summary>
    /// The user who provided the feedback.
    /// </summary>
    public Guid ProvidedByUserId { get; set; }

    /// <summary>
    /// Source of the feedback: 0=Customer, 1=Worker.
    /// </summary>
    public int Source { get; set; }

    /// <summary>
    /// Rating provided (1-5, or 0 if not rated).
    /// </summary>
    public decimal Rating { get; set; }

    /// <summary>
    /// The feedback comment.
    /// </summary>
    public string? Comment { get; set; }

    /// <summary>
    /// Type of feedback.
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// Whether the feedback is actionable for improvements.
    /// </summary>
    public bool IsActionable { get; set; }

    /// <summary>
    /// When the feedback was created.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }
}

/// <summary>
/// Response containing paginated list of feedback.
/// </summary>
public class FeedbackPageResponse
{
    /// <summary>
    /// The list of feedback items.
    /// </summary>
    public IReadOnlyList<FeedbackResponse>? Items { get; set; }

    /// <summary>
    /// Total count of all feedback (not just this page).
    /// </summary>
    public int TotalCount { get; set; }
}

/// <summary>
/// Response containing feedback statistics for a tenant.
/// </summary>
public class FeedbackStatisticsResponse
{
    /// <summary>
    /// Total number of feedback submissions.
    /// </summary>
    public int TotalFeedbackCount { get; set; }

    /// <summary>
    /// Overall average rating across all feedback.
    /// </summary>
    public decimal AverageRating { get; set; }

    /// <summary>
    /// Count of feedback from customers.
    /// </summary>
    public int CustomerFeedbackCount { get; set; }

    /// <summary>
    /// Count of feedback from workers.
    /// </summary>
    public int WorkerFeedbackCount { get; set; }

    /// <summary>
    /// Average rating from customers.
    /// </summary>
    public decimal AverageCustomerRating { get; set; }

    /// <summary>
    /// Average rating from workers.
    /// </summary>
    public decimal AverageWorkerRating { get; set; }

    /// <summary>
    /// Count of actionable feedback items.
    /// </summary>
    public int ActionableFeedbackCount { get; set; }

    /// <summary>
    /// Breakdown of feedback count by type (key = type enum value, value = count).
    /// </summary>
    public Dictionary<int, int>? FeedbackCountByType { get; set; }
}
