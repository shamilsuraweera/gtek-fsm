namespace GTEK.FSM.MobileApp.Workflows;

using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

internal sealed record CustomerRequestSnapshot(
    string Id,
    string Title,
    string Summary,
    string EtaText,
    string StatusLabel,
    int CurrentStage);

internal sealed record CustomerRequestSubmissionPlan(bool IsValid, string Title, string FeedbackMessage);

internal sealed record CustomerRequestDetailPresentation(
    string LifecycleText,
    string WorkerText,
    string JobText,
    string UpdatedText,
    IReadOnlyList<string> TimelineLines);

internal static class CustomerRequestJourney
{
    private const int MinimumDetailsLength = 10;
    private const int MaximumTitleLength = 180;

    public static CustomerRequestSubmissionPlan PlanSubmission(string categoryName, string details)
    {
        if (string.IsNullOrWhiteSpace(categoryName))
        {
            return new CustomerRequestSubmissionPlan(false, string.Empty, "Select a category before submitting.");
        }

        var normalizedDetails = details?.Trim() ?? string.Empty;
        if (normalizedDetails.Length < MinimumDetailsLength)
        {
            return new CustomerRequestSubmissionPlan(false, string.Empty, "Provide at least 10 characters of details.");
        }

        var title = BuildRequestTitle(categoryName, normalizedDetails);
        if (title.Length > MaximumTitleLength)
        {
            return new CustomerRequestSubmissionPlan(false, string.Empty, "Combined category and details must be 180 characters or less.");
        }

        return new CustomerRequestSubmissionPlan(true, title, string.Empty);
    }

    public static CustomerRequestSnapshot BuildFallbackCreatedRequest(CreateServiceRequestResponse createdRequest)
    {
        var status = createdRequest.Status;
        return new CustomerRequestSnapshot(
            Id: createdRequest.RequestId,
            Title: createdRequest.Title,
            Summary: createdRequest.Title,
            EtaText: $"Updated {createdRequest.UpdatedAtUtc:g}",
            StatusLabel: status,
            CurrentStage: ResolveStageIndex(status));
    }

    public static string ResolvePendingRequestId(string pendingRequestId, IEnumerable<CustomerRequestSnapshot> requests)
    {
        if (string.IsNullOrWhiteSpace(pendingRequestId))
        {
            return string.Empty;
        }

        return requests
            .FirstOrDefault(request => string.Equals(request.Id, pendingRequestId, StringComparison.OrdinalIgnoreCase))?
            .Id ?? string.Empty;
    }

    public static CustomerRequestSnapshot SyncDetail(CustomerRequestSnapshot request, GetServiceRequestDetailResponse detail)
    {
        var resolvedStatus = detail.Status ?? request.StatusLabel;
        return request with
        {
            StatusLabel = resolvedStatus,
            CurrentStage = ResolveStageIndex(resolvedStatus),
            EtaText = $"Updated {detail.UpdatedAtUtc:g}",
        };
    }

    public static CustomerRequestSnapshot ApplyStatusUpdate(CustomerRequestSnapshot request, ServiceRequestStatusUpdatedEvent payload)
    {
        var updatedStatus = MobileOperationalRealtimeMapper.NormalizeStatus(payload.CurrentStatus);
        return request with
        {
            StatusLabel = updatedStatus,
            CurrentStage = ResolveStageIndex(updatedStatus),
            EtaText = $"Updated {payload.UpdatedAtUtc:g}",
        };
    }

    public static CustomerRequestDetailPresentation BuildUnavailableDetailPresentation(string message)
    {
        return new CustomerRequestDetailPresentation(
            LifecycleText: string.Empty,
            WorkerText: $"Assigned worker: unavailable ({message})",
            JobText: "Active job: unavailable",
            UpdatedText: string.Empty,
            TimelineLines: Array.Empty<string>());
    }

    public static CustomerRequestDetailPresentation BuildDetailPresentation(GetServiceRequestDetailResponse detail)
    {
        return new CustomerRequestDetailPresentation(
            LifecycleText: $"Current lifecycle: {detail.Status}",
            WorkerText: string.IsNullOrWhiteSpace(detail.AssignedWorkerUserId)
                ? "Assigned worker: Not assigned yet"
                : $"Assigned worker ID: {detail.AssignedWorkerUserId}",
            JobText: string.IsNullOrWhiteSpace(detail.ActiveJobId)
                ? "Active job: No active job yet"
                : $"Active job {detail.ActiveJobId} • Status: {detail.ActiveJobStatus ?? "Unknown"}",
            UpdatedText: $"Last updated {detail.UpdatedAtUtc:g}",
            TimelineLines: BuildTimelineLines(detail.Timeline));
    }

    public static IReadOnlyList<string> BuildTimelineLines(IReadOnlyList<DetailTimelineItemResponse> timeline)
    {
        if (timeline.Count == 0)
        {
            return new[] { "No additional activity yet." };
        }

        return timeline
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .Select(item =>
            {
                var actorText = string.IsNullOrWhiteSpace(item.ActorUserId) ? string.Empty : $" • Actor {item.ActorUserId}";
                return $"{item.OccurredAtUtc:g} • {item.EventType ?? "UPDATE"} • {item.Message ?? "Activity recorded"}{actorText}";
            })
            .ToArray();
    }

    private static string BuildRequestTitle(string categoryName, string details)
    {
        return $"{categoryName}: {details}";
    }

    private static int ResolveStageIndex(string stage)
    {
        return MobileOperationalRealtimeMapper.ResolveRequestStageIndex(stage);
    }
}