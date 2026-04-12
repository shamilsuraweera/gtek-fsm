using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.Automation;

internal interface IOperationalAutomationRule
{
    string RuleKey { get; }

    Task<OperationalAutomationRuleResult> ExecuteAsync(
        Tenant tenant,
        int maxActions,
        OperationalAutomationSettings settings,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken);
}