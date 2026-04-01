using GTEK.FSM.Shared.Contracts.Vocabulary;

namespace GTEK.FSM.WebPortal.Models;

public static class RequestStagePresentation
{
    public static string GetLabel(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated)
        {
            return "Escalated";
        }

        return stage switch
        {
            RequestStage.New => "New",
            RequestStage.Assigned => "Assigned",
            RequestStage.InProgress => "Active",
            RequestStage.OnHold => "Waiting",
            RequestStage.Completed => "Completed",
            RequestStage.Cancelled => "Cancelled",
            _ => "Unknown",
        };
    }

    public static string GetCssClass(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated)
        {
            return "escalated";
        }

        return stage switch
        {
            RequestStage.New => "new",
            RequestStage.Assigned => "assigned",
            RequestStage.InProgress => "active",
            RequestStage.OnHold => "waiting",
            RequestStage.Completed => "completed",
            RequestStage.Cancelled => "cancelled",
            _ => "unknown",
        };
    }

    public static string GetIcon(RequestStage stage, bool isEscalated = false)
    {
        if (isEscalated)
        {
            return "⚠️";
        }

        return stage switch
        {
            RequestStage.New => "📋",
            RequestStage.Assigned => "👤",
            RequestStage.InProgress => "🔧",
            RequestStage.OnHold => "⏳",
            RequestStage.Completed => "✅",
            RequestStage.Cancelled => "❌",
            _ => "?",
        };
    }

    public static string? MapPipelineStage(RequestStage stage)
    {
        return stage switch
        {
            RequestStage.New => "Intake",
            RequestStage.Assigned => "Dispatch",
            RequestStage.InProgress => "In Progress",
            RequestStage.OnHold => "Assessment",
            RequestStage.Completed => null,
            RequestStage.Cancelled => null,
            _ => null,
        };
    }

    public static string MapWorkspaceStage(RequestStage stage)
    {
        return MapPipelineStage(stage) ?? "Closed";
    }
}
