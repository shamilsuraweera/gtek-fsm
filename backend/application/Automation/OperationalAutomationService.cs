using GTEK.FSM.Backend.Application.Persistence.Repositories;

namespace GTEK.FSM.Backend.Application.Automation;

internal sealed class OperationalAutomationService : IOperationalAutomationService
{
    private readonly ITenantRepository tenantRepository;
    private readonly IReadOnlyList<IOperationalAutomationRule> rules;

    public OperationalAutomationService(
        ITenantRepository tenantRepository,
        IEnumerable<IOperationalAutomationRule> rules)
    {
        this.tenantRepository = tenantRepository;
        this.rules = rules.ToArray();
    }

    public async Task<OperationalAutomationRunResult> ExecuteAsync(
        OperationalAutomationSettings settings,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var tenants = await this.tenantRepository.ListAsync(cancellationToken);
        var ruleTotals = new Dictionary<string, OperationalAutomationRuleResult>(StringComparer.Ordinal);
        var executedCount = 0;
        var skippedCount = 0;

        foreach (var tenant in tenants)
        {
            var remainingBudget = settings.GetMaxActionsPerTenantPerRun();

            foreach (var rule in this.rules)
            {
                if (remainingBudget <= 0)
                {
                    break;
                }

                var result = await rule.ExecuteAsync(tenant, remainingBudget, settings, occurredAtUtc, cancellationToken);
                remainingBudget = Math.Max(0, remainingBudget - result.ExecutedCount);
                executedCount += result.ExecutedCount;
                skippedCount += result.SkippedCount;

                if (ruleTotals.TryGetValue(result.RuleKey, out var existing))
                {
                    ruleTotals[result.RuleKey] = existing with
                    {
                        ExecutedCount = existing.ExecutedCount + result.ExecutedCount,
                        SkippedCount = existing.SkippedCount + result.SkippedCount,
                    };
                }
                else
                {
                    ruleTotals[result.RuleKey] = result;
                }
            }
        }

        return new OperationalAutomationRunResult(
            occurredAtUtc,
            tenants.Count,
            executedCount,
            skippedCount,
            ruleTotals.Values.OrderBy(x => x.RuleKey, StringComparer.Ordinal).ToArray());
    }
}