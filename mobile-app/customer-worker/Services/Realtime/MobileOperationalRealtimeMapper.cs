namespace GTEK.FSM.MobileApp.Services.Realtime;

public static class MobileOperationalRealtimeMapper
{
    public static string NormalizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return "Available";
        }

        var normalized = status.Trim().ToLowerInvariant();
        return normalized switch
        {
            "new" => "Available",
            "pending" => "Available",
            "scheduled" => "Scheduled",
            "assigned" => "Assigned",
            "accepted" => "Accepted",
            "onroute" => "On Route",
            "on_route" => "On Route",
            "onsite" => "On Site",
            "on_site" => "On Site",
            "inprogress" => "In Progress",
            "in_progress" => "In Progress",
            "completed" => "Completed",
            _ => status,
        };
    }

    public static int ResolveRequestStageIndex(string status)
    {
        var normalized = NormalizeStatus(status).ToLowerInvariant();
        return normalized switch
        {
            "available" => 0,
            "submitted" => 0,
            "scheduled" => 1,
            "assigned" => 1,
            "on route" => 2,
            "in progress" => 2,
            "completed" => 3,
            _ => 0,
        };
    }

    public static Color ResolveRequestStageColor(string status)
    {
        var normalized = NormalizeStatus(status).ToLowerInvariant();
        return normalized switch
        {
            "completed" => Color.FromArgb("#166534"),
            "scheduled" => Color.FromArgb("#166534"),
            "assigned" => Color.FromArgb("#0F6ABD"),
            "on route" => Color.FromArgb("#B45309"),
            "in progress" => Color.FromArgb("#0F6ABD"),
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