namespace GTEK.FSM.Backend.Application.Identity;

public interface IAuthorizationDecisionAuditSink
{
    Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default);
}
