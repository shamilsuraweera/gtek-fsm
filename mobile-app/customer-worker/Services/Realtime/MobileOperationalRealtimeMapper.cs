namespace GTEK.FSM.MobileApp.Services.Realtime;

using GTEK.FSM.Shared.Contracts.Vocabulary;

public static class MobileOperationalRealtimeMapper
{
    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return RequestLifecycleTerminology.GetDisplayLabel(RequestStage.New.ToString());
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "accepted" => "Accepted",
            "onroute" => "On Route",
            "on_route" => "On Route",
            "onsite" => "On Site",
            "on_site" => "On Site",
            _ => RequestLifecycleTerminology.GetDisplayLabel(status),
        };
    }

    public static int ResolveRequestStageIndex(string status)
    {
        return RequestLifecycleTerminology.ResolveStageIndex(status);
    }

    public static Color ResolveRequestStageColor(string status)
    {
        var normalized = RequestLifecycleTerminology.NormalizeStatus(status);
        return normalized switch
        {
            nameof(RequestStage.Completed) => Color.FromArgb("#34D399"),
            nameof(RequestStage.Cancelled) => Color.FromArgb("#94A3B8"),
            nameof(RequestStage.Assigned) => Color.FromArgb("#F4B266"),
            nameof(RequestStage.InProgress) => Color.FromArgb("#F59E0B"),
            nameof(RequestStage.OnHold) => Color.FromArgb("#FB923C"),
            _ => Color.FromArgb("#94A3B8"),
        };
    }

    public static Color ResolveJobStatusColor(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized switch
        {
            "Completed" => Color.FromArgb("#34D399"),
            "In Progress" => Color.FromArgb("#F59E0B"),
            "On Site" => Color.FromArgb("#F4B266"),
            "On Route" => Color.FromArgb("#FB923C"),
            "Accepted" => Color.FromArgb("#60A5FA"),
            _ => Color.FromArgb("#94A3B8"),
        };
    }

    public static bool IsAcceptedStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is "Accepted" or "On Route" or "On Site" or "In Progress" or "Completed";
    }
}