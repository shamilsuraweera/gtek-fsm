using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Models;

namespace GTEK.FSM.WebPortal.Services.Realtime;

public static class OperationalRealtimeMapper
{
    public static bool TryParseRequestStage(string value, out RequestStage stage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            stage = RequestStage.New;
            return false;
        }

        var normalized = value
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);

        return Enum.TryParse(normalized, ignoreCase: true, out stage);
    }

    public static string? MapPipelineStage(RequestStage stage)
    {
        return RequestStagePresentation.MapPipelineStage(stage);
    }

    public static string MapWorkspaceStage(RequestStage stage)
    {
        return RequestStagePresentation.MapWorkspaceStage(stage);
    }

    public static RequestStage MapAssignmentStage(string assignmentStatus)
    {
        return TryParseRequestStage(assignmentStatus, out var parsedStage)
            ? parsedStage
            : RequestStage.Assigned;
    }
}
