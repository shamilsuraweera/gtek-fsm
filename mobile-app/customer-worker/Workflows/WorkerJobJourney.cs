namespace GTEK.FSM.MobileApp.Workflows;

using GTEK.FSM.MobileApp.Services.Realtime;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;

internal sealed record WorkerJobSnapshot(
    string Id,
    string RequestId,
    string Title,
    string Description,
    string StatusLabel,
    bool Accepted,
    string RequestRowVersion);

internal static class WorkerJobJourney
{
    public static string ResolvePendingJobId(string pendingJobId, string pendingRequestId, IEnumerable<WorkerJobSnapshot> jobs)
    {
        if (!string.IsNullOrWhiteSpace(pendingJobId))
        {
            var byJobId = jobs.FirstOrDefault(job => string.Equals(job.Id, pendingJobId, StringComparison.OrdinalIgnoreCase));
            if (byJobId is not null)
            {
                return byJobId.Id;
            }
        }

        if (string.IsNullOrWhiteSpace(pendingRequestId))
        {
            return string.Empty;
        }

        return jobs
            .FirstOrDefault(job => string.Equals(job.RequestId, pendingRequestId, StringComparison.OrdinalIgnoreCase))?
            .Id ?? string.Empty;
    }

    public static WorkerJobSnapshot MergeExecutionContext(
        WorkerJobSnapshot current,
        GetJobDetailResponse detail,
        GetServiceRequestDetailResponse requestDetail)
    {
        var requestId = detail.RequestId ?? current.RequestId;
        var requestStatus = requestDetail?.Status ?? detail.RequestStatus ?? current.StatusLabel;
        var normalizedStatus = NormalizeStatus(requestStatus);

        return current with
        {
            RequestId = requestId,
            RequestRowVersion = requestDetail?.RowVersion ?? current.RequestRowVersion,
            Title = detail.RequestTitle ?? current.Title,
            Description = detail.RequestTitle ?? current.Description,
            StatusLabel = normalizedStatus,
            Accepted = IsAcceptedStatus(normalizedStatus),
        };
    }

    public static WorkerJobSnapshot ApplyTransition(WorkerJobSnapshot current, TransitionServiceRequestStatusResponse transition)
    {
        var normalizedStatus = NormalizeStatus(transition.CurrentStatus);
        return current with
        {
            StatusLabel = normalizedStatus,
            RequestRowVersion = transition.RowVersion ?? current.RequestRowVersion,
            Accepted = IsAcceptedStatus(normalizedStatus),
        };
    }

    public static WorkerJobSnapshot ApplyAssignmentUpdate(WorkerJobSnapshot current, string assignmentStatus)
    {
        var normalizedStatus = NormalizeStatus(assignmentStatus);
        return current with
        {
            StatusLabel = normalizedStatus,
            Accepted = IsAcceptedStatus(normalizedStatus),
        };
    }

    public static string BuildTransitionFailureMessage(bool isConflict, string message)
    {
        return isConflict
            ? $"Conflict detected: {message}. Refreshing latest state."
            : $"Status update failed: {message}";
    }

    public static string BuildTransitionSuccessMessage(string successMessage, string jobId, DateTime occurredAtLocal)
    {
        return $"{successMessage} for {jobId} at {occurredAtLocal:t}.";
    }

    public static string ToApiStatus(string status)
    {
        var normalized = NormalizeStatus(status).ToLowerInvariant();
        return normalized switch
        {
            "assigned" => "Assigned",
            "in progress" => "InProgress",
            "on hold" => "OnHold",
            "completed" => "Completed",
            _ => "Assigned",
        };
    }

    private static string NormalizeStatus(string status)
    {
        return MobileOperationalRealtimeMapper.NormalizeStatus(status);
    }

    private static bool IsAcceptedStatus(string status)
    {
        return MobileOperationalRealtimeMapper.IsAcceptedStatus(status);
    }
}