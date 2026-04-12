using GTEK.FSM.Backend.Application.Identity;
using System.Diagnostics.Metrics;

using Microsoft.Extensions.Logging;

namespace GTEK.FSM.Backend.Infrastructure.Identity;

public sealed class AuthorizationDecisionAuditLogger(ILogger<AuthorizationDecisionAuditLogger> logger) : IAuthorizationDecisionAuditSink
{
    private static readonly Meter Meter = new("GTEK.FSM.Backend.Infrastructure", "1.0.0");
    private static readonly Counter<long> AuthorizationDecisionCounter = Meter.CreateCounter<long>(
        name: "authorization_decisions_total",
        unit: "decisions",
        description: "Total authorization decisions audited.");

    public Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        AuthorizationDecisionCounter.Add(1,
            new KeyValuePair<string, object?>("action", auditEvent.Action),
            new KeyValuePair<string, object?>("outcome", auditEvent.Outcome),
            new KeyValuePair<string, object?>("reason", auditEvent.Reason));

        logger.LogInformation(
            "authorization_decision action={Action} outcome={Outcome} reason={Reason} userId={UserId} sourceTenantId={SourceTenantId} targetTenantId={TargetTenantId} occurredAtUtc={OccurredAtUtc}",
            auditEvent.Action,
            auditEvent.Outcome,
            auditEvent.Reason,
            auditEvent.UserId,
            auditEvent.SourceTenantId,
            auditEvent.TargetTenantId,
            auditEvent.OccurredAtUtc);

        return Task.CompletedTask;
    }
}
