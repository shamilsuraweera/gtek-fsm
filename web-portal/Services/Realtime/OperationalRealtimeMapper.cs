using GTEK.FSM.WebPortal.Models;

namespace GTEK.FSM.WebPortal.Services.Realtime;

public static class OperationalRealtimeMapper
{
    public static bool TryParseRequestStatus(string value, out RequestStatus status)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            status = RequestStatus.New;
            return false;
        }

        var normalized = value
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse(normalized, ignoreCase: true, out status);
    }

    public static string? MapPipelineStage(RequestStatus status)
    {
        return status switch
        {
            RequestStatus.New => "Intake",
            RequestStatus.Assessing => "Assessment",
            RequestStatus.Assigned => "Dispatch",
            RequestStatus.Active => "In Progress",
            RequestStatus.Waiting => "Assessment",
            RequestStatus.Escalated => "Dispatch",
            RequestStatus.OnHold => "Assessment",
            RequestStatus.Completed => null,
            RequestStatus.Cancelled => null,
            _ => null,
        };
    }

    public static string MapWorkspaceStage(RequestStatus status)
    {
        return MapPipelineStage(status) ?? "Closed";
    }

    public static RequestStatus MapAssignmentStatus(string assignmentStatus)
    {
        return TryParseRequestStatus(assignmentStatus, out var parsedStatus)
            ? parsedStatus
            : RequestStatus.Assigned;
    }
}