using System.Text.Json;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;

namespace GTEK.FSM.Backend.Application.Automation;

internal sealed class SubscriptionExpiryReminderAutomationRule : IOperationalAutomationRule
{
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IAuditLogRepository auditLogRepository;
    private readonly IAuditLogWriter auditLogWriter;

    public SubscriptionExpiryReminderAutomationRule(
        ISubscriptionRepository subscriptionRepository,
        IAuditLogRepository auditLogRepository,
        IAuditLogWriter auditLogWriter)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.auditLogRepository = auditLogRepository;
        this.auditLogWriter = auditLogWriter;
    }

    public string RuleKey => "subscription_expiry_reminder";

    public async Task<OperationalAutomationRuleResult> ExecuteAsync(
        Tenant tenant,
        int maxActions,
        OperationalAutomationSettings settings,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (!settings.EnableSubscriptionExpiryReminderWorkflow || maxActions <= 0)
        {
            return new OperationalAutomationRuleResult(this.RuleKey, 0, 0);
        }

        var subscriptions = await this.subscriptionRepository.ListByTenantAsync(tenant.Id, cancellationToken);
        var reminderCutoff = occurredAtUtc.UtcDateTime.AddDays(settings.GetSubscriptionExpiryReminderDays());

        var candidates = subscriptions
            .Where(x => x.EndsOnUtc.HasValue)
            .Where(x => x.EndsOnUtc!.Value >= occurredAtUtc.UtcDateTime)
            .Where(x => x.EndsOnUtc!.Value <= reminderCutoff)
            .OrderBy(x => x.EndsOnUtc)
            .ToArray();

        var executedCount = 0;
        var skippedCount = 0;

        foreach (var subscription in candidates)
        {
            if (executedCount >= maxActions)
            {
                break;
            }

            const string action = "Automation:SubscriptionExpiryReminder";

            var duplicateCount = await this.auditLogRepository.CountAsync(
                new AuditLogQuerySpecification(
                    tenant.Id,
                    EntityType: "Subscription",
                    EntityId: subscription.Id,
                    Action: action,
                    Outcome: "Success",
                    OccurredFromUtc: occurredAtUtc.Subtract(settings.GetReminderCooldown())),
                cancellationToken);

            if (duplicateCount > 0)
            {
                skippedCount++;
                continue;
            }

            var daysRemaining = (int)Math.Ceiling((subscription.EndsOnUtc!.Value - occurredAtUtc.UtcDateTime).TotalDays);
            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = null,
                TenantId = tenant.Id,
                EntityType = "Subscription",
                EntityId = subscription.Id,
                Action = action,
                Outcome = "Success",
                OccurredAtUtc = occurredAtUtc,
                Details = JsonSerializer.Serialize(new
                {
                    rule = this.RuleKey,
                    subscriptionId = subscription.Id,
                    planCode = subscription.PlanCode,
                    endsOnUtc = subscription.EndsOnUtc,
                    daysRemaining,
                }),
            };

            await this.auditLogWriter.WriteAsync(auditLog, cancellationToken);
            executedCount++;
        }

        return new OperationalAutomationRuleResult(this.RuleKey, executedCount, skippedCount);
    }
}