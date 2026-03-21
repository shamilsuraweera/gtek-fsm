namespace GTEK.FSM.WebPortal.Components.Operations;

public sealed record QueueListItem(
    string Reference,
    string Customer,
    string Stage,
    string Priority,
    string Summary,
    DateTime UpdatedAtUtc);
