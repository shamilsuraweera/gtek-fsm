using GTEK.FSM.Backend.Application.Identity;

using Microsoft.Extensions.Logging;

namespace GTEK.FSM.Backend.Infrastructure.Identity;

public sealed class AuthorizationDecisionAuditLogger(ILogger<AuthorizationDecisionAuditLogger> logger) : IAuthorizationDecisionAuditSink
{
    public Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
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
