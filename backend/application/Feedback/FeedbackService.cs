using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;

namespace GTEK.FSM.Backend.Application.Feedback;

internal sealed class FeedbackService : IFeedbackService
{
    private readonly IFeedbackRepository feedbackRepository;
    private readonly IUnitOfWork unitOfWork;

    public FeedbackService(IFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork)
    {
        this.feedbackRepository = feedbackRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Guid> SubmitFeedbackAsync(
        Guid tenantId,
        Guid jobId,
        Guid serviceRequestId,
        Guid providedByUserId,
        FeedbackSource source,
        decimal? rating,
        string? comment,
        FeedbackType type,
        CancellationToken cancellationToken = default)
    {
        var feedbackId = Guid.NewGuid();
        var feedback = new Domain.Aggregates.Feedback(feedbackId, tenantId, jobId, serviceRequestId, providedByUserId, source);

        if (rating.HasValue)
        {
            feedback.SetRating(rating.Value);
        }

        if (!string.IsNullOrEmpty(comment))
        {
            feedback.SetComment(comment);
        }

        feedback.SetType(type);

        // Determine if feedback is actionable: low ratings or contains known issue keywords
        var isActionable = (rating.HasValue && rating < 3) || IsActionableFeedback(comment);
        feedback.SetActionable(isActionable);

        await this.feedbackRepository.AddAsync(feedback, cancellationToken);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);

        return feedbackId;
    }

    public async Task<IReadOnlyList<FeedbackData>> GetFeedbackForServiceRequestAsync(
        Guid tenantId,
        Guid serviceRequestId,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await this.feedbackRepository.GetByServiceRequestAsync(tenantId, serviceRequestId, cancellationToken);
        return feedbacks.Select(MapToFeedbackData).ToList();
    }

    public async Task<IReadOnlyList<FeedbackData>> GetFeedbackForJobAsync(
        Guid tenantId,
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await this.feedbackRepository.GetByJobAsync(tenantId, jobId, cancellationToken);
        return feedbacks.Select(MapToFeedbackData).ToList();
    }

    public async Task<(IReadOnlyList<FeedbackData> Items, int TotalCount)> QueryFeedbackAsync(
        Guid tenantId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await this.feedbackRepository.QueryAsync(tenantId, skip, take, cancellationToken);
        var total = await this.feedbackRepository.CountAsync(tenantId, cancellationToken);
        var items = feedbacks.Select(MapToFeedbackData).ToList();
        return (items, total);
    }

    public async Task<IReadOnlyList<FeedbackData>> GetActionableFeedbackAsync(
        Guid tenantId,
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await this.feedbackRepository.GetActionableFeedbackAsync(tenantId, skip, take, cancellationToken);
        return feedbacks.Select(MapToFeedbackData).ToList();
    }

    public async Task<FeedbackStatistics> GetFeedbackStatisticsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var feedbacks = await this.feedbackRepository.QueryAsync(tenantId, 0, int.MaxValue, cancellationToken);
        var countBySource = await this.feedbackRepository.GetFeedbackCountBySourceAsync(tenantId, cancellationToken);

        var customerFeedbacks = feedbacks.Where(f => f.Source == FeedbackSource.Customer).ToList();
        var workerFeedbacks = feedbacks.Where(f => f.Source == FeedbackSource.Worker).ToList();

        var averageCustomerRating = customerFeedbacks.Count > 0 && customerFeedbacks.Any(f => f.Rating > 0)
            ? decimal.Round(customerFeedbacks.Where(f => f.Rating > 0).Average(f => f.Rating), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var averageWorkerRating = workerFeedbacks.Count > 0 && workerFeedbacks.Any(f => f.Rating > 0)
            ? decimal.Round(workerFeedbacks.Where(f => f.Rating > 0).Average(f => f.Rating), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var averageRating = feedbacks.Count > 0 && feedbacks.Any(f => f.Rating > 0)
            ? decimal.Round(feedbacks.Where(f => f.Rating > 0).Average(f => f.Rating), 2, MidpointRounding.AwayFromZero)
            : 0m;

        var feedbackCountByType = feedbacks
            .GroupBy(f => f.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        return new FeedbackStatistics
        {
            TotalFeedbackCount = feedbacks.Count,
            AverageRating = averageRating,
            CustomerFeedbackCount = countBySource.GetValueOrDefault((int)FeedbackSource.Customer, 0),
            WorkerFeedbackCount = countBySource.GetValueOrDefault((int)FeedbackSource.Worker, 0),
            AverageCustomerRating = averageCustomerRating,
            AverageWorkerRating = averageWorkerRating,
            ActionableFeedbackCount = feedbacks.Count(f => f.IsActionable),
            FeedbackCountByType = feedbackCountByType
        };
    }

    private static FeedbackData MapToFeedbackData(Domain.Aggregates.Feedback feedback)
    {
        return new FeedbackData
        {
            Id = feedback.Id,
            JobId = feedback.JobId,
            ServiceRequestId = feedback.ServiceRequestId,
            ProvidedByUserId = feedback.ProvidedByUserId,
            Source = feedback.Source,
            Rating = feedback.Rating,
            Comment = feedback.Comment,
            Type = feedback.Type,
            IsActionable = feedback.IsActionable,
            CreatedAtUtc = feedback.CreatedAtUtc
        };
    }

    private static bool IsActionableFeedback(string? comment)
    {
        if (string.IsNullOrEmpty(comment))
        {
            return false;
        }

        var actionableKeywords = new[]
        {
            "issue", "problem", "error", "broken", "failed", "mistake", "late", "slow", "rude", "unprofessional",
            "incomplete", "missing", "wrong", "incorrect", "damaged", "unsafe", "unhappy", "disappointed"
        };

        var lowerComment = comment.ToLowerInvariant();
        return actionableKeywords.Any(keyword => lowerComment.Contains(keyword));
    }
}
