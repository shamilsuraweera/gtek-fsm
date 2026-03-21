namespace GTEK.FSM.WebPortal.Components.Operations;

public sealed record OperationsBoardCard(
    string Reference,
    string Title,
    string Priority,
    string Assignee,
    DateTime UpdatedAtUtc);
