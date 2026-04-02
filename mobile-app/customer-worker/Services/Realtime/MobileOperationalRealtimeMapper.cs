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
            nameof(RequestStage.Completed) => Color.FromArgb("#166534"),
            nameof(RequestStage.Cancelled) => Color.FromArgb("#6B7280"),
            nameof(RequestStage.Assigned) => Color.FromArgb("#0F6ABD"),
            nameof(RequestStage.InProgress) => Color.FromArgb("#0F6ABD"),
            nameof(RequestStage.OnHold) => Color.FromArgb("#B45309"),
            _ => Color.FromArgb("#6B7280"),
        };
    }

    public static Color ResolveJobStatusColor(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized switch
        {
            "Completed" => Color.FromArgb("#166534"),
            "In Progress" => Color.FromArgb("#0F6ABD"),
            "On Site" => Color.FromArgb("#0F6ABD"),
            "On Route" => Color.FromArgb("#B45309"),
            "Accepted" => Color.FromArgb("#166534"),
            _ => Color.FromArgb("#6B7280"),
        };
    }

    public static bool IsAcceptedStatus(string status)
    {
        var normalized = NormalizeStatus(status);
        return normalized is "Accepted" or "On Route" or "On Site" or "In Progress" or "Completed";
    }
}