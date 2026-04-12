using System.Text.Json;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.Automation;

internal sealed class SlaReminderAutomationRule : IOperationalAutomationRule
{
    private static readonly ServiceRequestStatus[] EligibleStatuses =
    [
        ServiceRequestStatus.New,
        ServiceRequestStatus.Assigned,
        ServiceRequestStatus.InProgress,
        ServiceRequestStatus.OnHold,
    ];

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IAuditLogRepository auditLogRepository;
    private readonly IAuditLogWriter auditLogWriter;

    public SlaReminderAutomationRule(
        IServiceRequestRepository serviceRequestRepository,
        IAuditLogRepository auditLogRepository,
        IAuditLogWriter auditLogWriter)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.auditLogRepository = auditLogRepository;
        this.auditLogWriter = auditLogWriter;
    }

    public string RuleKey => "sla_reminder";

    public async Task<OperationalAutomationRuleResult> ExecuteAsync(
        Tenant tenant,
        int maxActions,
        OperationalAutomationSettings settings,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        if (!settings.EnableSlaReminderWorkflow || maxActions <= 0)
        {
            return new OperationalAutomationRuleResult(this.RuleKey, 0, 0);
        }

        var requests = await this.serviceRequestRepository.ListByTenantAsync(tenant.Id, cancellationToken);
        var candidates = requests
            .Where(x => EligibleStatuses.Contains(x.Status))
            .SelectMany(CreateCandidates)
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.DueAtUtc)
            .ToArray();

        var executedCount = 0;
        var skippedCount = 0;

        foreach (var candidate in candidates)
        {
            if (executedCount >= maxActions)
            {
                break;
            }

            var duplicateCount = await this.auditLogRepository.CountAsync(
                new AuditLogQuerySpecification(
                    tenant.Id,
                    EntityType: "ServiceRequest",
                    EntityId: candidate.Request.Id,
                    Action: candidate.Action,
                    Outcome: "Success",
                    OccurredFromUtc: occurredAtUtc.Subtract(settings.GetReminderCooldown())),
                cancellationToken);

            if (duplicateCount > 0)
            {
                skippedCount++;
                continue;
            }

            var auditLog = new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = null,
                TenantId = tenant.Id,
                EntityType = "ServiceRequest",
                EntityId = candidate.Request.Id,
                Action = candidate.Action,
                Outcome = "Success",
                OccurredAtUtc = occurredAtUtc,
                Details = JsonSerializer.Serialize(new
                {
                    rule = this.RuleKey,
                    requestId = candidate.Request.Id,
                    requestStatus = candidate.Request.Status.ToString(),
                    slaDimension = candidate.Dimension,
                    slaState = candidate.State.ToString(),
                    dueAtUtc = candidate.DueAtUtc,
                }),
            };

            await this.auditLogWriter.WriteAsync(auditLog, cancellationToken);
            executedCount++;
        }

        return new OperationalAutomationRuleResult(this.RuleKey, executedCount, skippedCount);
    }

    private static IEnumerable<SlaReminderCandidate> CreateCandidates(ServiceRequest request)
    {
        if (request.ResponseDueAtUtc.HasValue && request.ResponseSlaState is SlaState.AtRisk or SlaState.Breached)
        {
            yield return new SlaReminderCandidate(request, "Response", request.ResponseSlaState, request.ResponseDueAtUtc.Value);
        }

        if (request.AssignmentDueAtUtc.HasValue && request.AssignmentSlaState is SlaState.AtRisk or SlaState.Breached)
        {
            yield return new SlaReminderCandidate(request, "Assignment", request.AssignmentSlaState, request.AssignmentDueAtUtc.Value);
        }

        if (request.CompletionDueAtUtc.HasValue && request.CompletionSlaState is SlaState.AtRisk or SlaState.Breached)
        {
            yield return new SlaReminderCandidate(request, "Completion", request.CompletionSlaState, request.CompletionDueAtUtc.Value);
        }
    }

    private sealed record SlaReminderCandidate(
        ServiceRequest Request,
        string Dimension,
        SlaState State,
        DateTime DueAtUtc)
    {
        public string Action => $"Automation:SlaReminder:{this.Dimension}:{this.State}";

        public int Priority => this.State == SlaState.Breached ? 2 : 1;
    }
}