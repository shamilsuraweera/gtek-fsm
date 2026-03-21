namespace GTEK.FSM.WebPortal.Components.Operations;

public sealed record OperationsBoardColumn(
    string Name,
    IReadOnlyList<OperationsBoardCard> Cards);
