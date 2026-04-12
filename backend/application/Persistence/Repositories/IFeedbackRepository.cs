using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Persistence.Repositories;

public interface IFeedbackRepository : IRepository<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>
{
    /// <summary>
    /// Gets a feedback record by ID for the specified tenant.
    /// </summary>
    Task<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback?> GetByIdAsync(Guid tenantId, Guid feedbackId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feedback for a service request within the tenant.
    /// </summary>
    Task<IReadOnlyList<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>> GetByServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all feedback for a job within the tenant.
    /// </summary>
    Task<IReadOnlyList<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>> GetByJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets feedback provided by a specific user within the tenant.
    /// </summary>
    Task<IReadOnlyList<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>> GetByProvidedByUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Queries feedback with pagination for a tenant.
    /// </summary>
    Task<IReadOnlyList<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>> QueryAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a count of all feedback for a tenant.
    /// </summary>
    Task<int> CountAsync(Guid tenantId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a service request.
    /// </summary>
    Task<decimal> GetAverageRatingForServiceRequestAsync(Guid tenantId, Guid serviceRequestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the average rating for a job.
    /// </summary>
    Task<decimal> GetAverageRatingForJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets actionable feedback for the tenant, sorted by creation date (most recent first).
    /// </summary>
    Task<IReadOnlyList<global::GTEK.FSM.Backend.Domain.Aggregates.Feedback>> GetActionableFeedbackAsync(Guid tenantId, int skip = 0, int take = 50, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an aggregate count of feedback by source (Customer/Worker) for the tenant.
    /// </summary>
    Task<Dictionary<int, int>> GetFeedbackCountBySourceAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
