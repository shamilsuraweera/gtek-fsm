using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Domain.Rules;

namespace GTEK.FSM.Backend.Domain.Aggregates;

/// <summary>
/// Feedback aggregate root.
/// Captures customer or worker feedback on completed service requests/jobs to refine operations and decisioning.
/// </summary>
public sealed class Feedback
{
    public Feedback(Guid id, Guid tenantId, Guid jobId, Guid serviceRequestId, Guid providedByUserId, FeedbackSource source)
    {
        this.Id = DomainGuards.RequiredId(id, nameof(id), "Feedback id cannot be empty.");
        this.TenantId = DomainGuards.RequiredId(tenantId, nameof(tenantId), "Feedback must belong to a tenant.");
        this.JobId = DomainGuards.RequiredId(jobId, nameof(jobId), "Job id cannot be empty.");
        this.ServiceRequestId = DomainGuards.RequiredId(serviceRequestId, nameof(serviceRequestId), "Service request id cannot be empty.");
        this.ProvidedByUserId = DomainGuards.RequiredId(providedByUserId, nameof(providedByUserId), "User id cannot be empty.");
        this.Source = source;
        this.Rating = 0m;
        this.Comment = string.Empty;
        this.Type = FeedbackType.ServiceQuality;
    }

    public Guid Id { get; }

    public Guid TenantId { get; }

    public Guid JobId { get; }

    public Guid ServiceRequestId { get; }

    public Guid ProvidedByUserId { get; }

    /// <summary>
    /// Source of the feedback (Customer or Worker).
    /// </summary>
    public FeedbackSource Source { get; }

    /// <summary>
    /// Numeric rating between 1 and 5. 0 if not rated.
    /// </summary>
    public decimal Rating { get; private set; }

    /// <summary>
    /// Free-form comment providing details about the feedback.
    /// </summary>
    public string Comment { get; private set; }

    /// <summary>
    /// Categorization of the feedback type.
    /// </summary>
    public FeedbackType Type { get; private set; }

    /// <summary>
    /// Flag indicating whether feedback is actionable for improvement.
    /// </summary>
    public bool IsActionable { get; private set; }

    public DateTime CreatedAtUtc { get; internal set; }

    public DateTime UpdatedAtUtc { get; internal set; }

    public byte[] RowVersion { get; internal set; } = Array.Empty<byte>();

    public bool IsDeleted { get; internal set; }

    /// <summary>
    /// Sets the numeric rating for the feedback (1-5 scale).
    /// </summary>
    public void SetRating(decimal rating)
    {
        if (rating < 0 || rating > 5)
        {
            throw new InvalidOperationException("Rating must be between 0 and 5.");
        }

        this.Rating = rating;
    }

    /// <summary>
    /// Sets the free-form comment, with length validation.
    /// </summary>
    public void SetComment(string? comment)
    {
        if (!string.IsNullOrEmpty(comment) && comment.Length > 1000)
        {
            throw new InvalidOperationException("Comment cannot exceed 1000 characters.");
        }

        this.Comment = comment ?? string.Empty;
    }

    /// <summary>
    /// Categorizes the feedback type.
    /// </summary>
    public void SetType(FeedbackType type)
    {
        this.Type = type;
    }

    /// <summary>
    /// Marks the feedback as actionable or not for improvement initiatives.
    /// </summary>
    public void SetActionable(bool isActionable)
    {
        this.IsActionable = isActionable;
    }
}
