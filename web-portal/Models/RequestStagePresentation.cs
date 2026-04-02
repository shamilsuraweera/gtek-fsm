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

        return RequestLifecycleTerminology.GetDisplayLabel(stage.ToString());
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
            RequestStage.InProgress => "in-progress",
            RequestStage.OnHold => "on-hold",
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
            RequestStage.New => "New",
            RequestStage.Assigned => "Assigned",
            RequestStage.InProgress => "In Progress",
            RequestStage.OnHold => "On Hold",
            RequestStage.Completed => null,
            RequestStage.Cancelled => null,
            _ => null,
        };
    }

    public static string MapWorkspaceStage(RequestStage stage)
    {
        return GetLabel(stage);
    }
}
