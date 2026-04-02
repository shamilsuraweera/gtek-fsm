using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal static class ServiceRequestSlaCalculator
{
    public static ServiceRequestSlaSnapshot Compute(
        ServiceRequest request,
        AssignmentStatus? assignmentStatus,
        DateTime nowUtc,
        ServiceRequestSlaOptions options)
    {
        var createdAtUtc = NormalizeAnchor(request.CreatedAtUtc, nowUtc);
        var updatedAtUtc = NormalizeAnchor(request.UpdatedAtUtc, nowUtc);

        var responseDueAtUtc = createdAtUtc.AddMinutes(Math.Max(1, options.ResponseMinutes));
        var assignmentAnchorUtc = updatedAtUtc > createdAtUtc ? updatedAtUtc : createdAtUtc;
        var assignmentDueAtUtc = assignmentAnchorUtc.AddMinutes(Math.Max(1, options.AssignmentMinutes));
        var completionDueAtUtc = createdAtUtc.AddMinutes(Math.Max(1, options.CompletionMinutes));

        var responseState = request.Status == ServiceRequestStatus.New
            ? Evaluate(nowUtc, createdAtUtc, responseDueAtUtc, options.AtRiskThresholdPercent)
            : SlaState.NotApplicable;

        var assignmentState = IsAssignmentApplicable(request.Status, assignmentStatus)
            ? Evaluate(nowUtc, assignmentAnchorUtc, assignmentDueAtUtc, options.AtRiskThresholdPercent)
            : SlaState.NotApplicable;

        var completionState = IsCompletionApplicable(request.Status)
            ? Evaluate(nowUtc, createdAtUtc, completionDueAtUtc, options.AtRiskThresholdPercent)
            : SlaState.NotApplicable;

        DateTime? nextDeadline = null;
        var activeDeadlines = new List<DateTime>(3);
        if (responseState != SlaState.NotApplicable)
        {
            activeDeadlines.Add(responseDueAtUtc);
        }

        if (assignmentState != SlaState.NotApplicable)
        {
            activeDeadlines.Add(assignmentDueAtUtc);
        }

        if (completionState != SlaState.NotApplicable)
        {
            activeDeadlines.Add(completionDueAtUtc);
        }

        if (activeDeadlines.Count > 0)
        {
            nextDeadline = activeDeadlines.Min();
        }

        return new ServiceRequestSlaSnapshot(
            ResponseDueAtUtc: responseState == SlaState.NotApplicable ? null : responseDueAtUtc,
            AssignmentDueAtUtc: assignmentState == SlaState.NotApplicable ? null : assignmentDueAtUtc,
            CompletionDueAtUtc: completionState == SlaState.NotApplicable ? null : completionDueAtUtc,
            ResponseSlaState: responseState,
            AssignmentSlaState: assignmentState,
            CompletionSlaState: completionState,
            NextSlaDeadlineAtUtc: nextDeadline);
    }

    private static DateTime NormalizeAnchor(DateTime value, DateTime fallbackUtc)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            value = DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        if (value.Year < 2000)
        {
            return fallbackUtc;
        }

        return value;
    }

    private static bool IsAssignmentApplicable(ServiceRequestStatus status, AssignmentStatus? assignmentStatus)
    {
        if (status != ServiceRequestStatus.Assigned)
        {
            return false;
        }

        if (assignmentStatus is AssignmentStatus.Accepted or AssignmentStatus.Completed)
        {
            return false;
        }

        return true;
    }

    private static bool IsCompletionApplicable(ServiceRequestStatus status)
    {
        return status is ServiceRequestStatus.Assigned or ServiceRequestStatus.InProgress or ServiceRequestStatus.OnHold;
    }

    private static SlaState Evaluate(DateTime nowUtc, DateTime startUtc, DateTime dueUtc, decimal atRiskThresholdPercent)
    {
        if (nowUtc >= dueUtc)
        {
            return SlaState.Breached;
        }

        var totalSeconds = Math.Max(1d, (dueUtc - startUtc).TotalSeconds);
        var elapsedSeconds = Math.Max(0d, (nowUtc - startUtc).TotalSeconds);
        var percent = (decimal)((elapsedSeconds / totalSeconds) * 100d);

        return percent >= atRiskThresholdPercent ? SlaState.AtRisk : SlaState.OnTrack;
    }
}
