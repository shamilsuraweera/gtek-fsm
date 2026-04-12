namespace GTEK.FSM.Backend.Domain.Enums;

/// <summary>
/// Indicates the source or role providing the feedback.
/// </summary>
public enum FeedbackSource
{
    /// <summary>Feedback provided by a customer who submitted/received the request.</summary>
    Customer = 0,

    /// <summary>Feedback provided by a worker who was assigned to complete the job.</summary>
    Worker = 1
}
