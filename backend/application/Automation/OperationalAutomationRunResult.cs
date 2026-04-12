namespace GTEK.FSM.Backend.Application.Automation;

public sealed record OperationalAutomationRuleResult(
    string RuleKey,
    int ExecutedCount,
    int SkippedCount);

public sealed record OperationalAutomationRunResult(
    DateTimeOffset OccurredAtUtc,
    int TenantCount,
    int ExecutedCount,
    int SkippedCount,
    IReadOnlyList<OperationalAutomationRuleResult> RuleResults);