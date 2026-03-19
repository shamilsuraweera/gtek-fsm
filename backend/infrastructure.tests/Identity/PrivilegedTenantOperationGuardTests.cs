using GTEK.FSM.Backend.Application.Identity;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class PrivilegedTenantOperationGuardTests
{
    [Fact]
    public async Task EvaluateAsync_WhenCrossTenantAndRoleNotPrivileged_RejectsAndAudits()
    {
        var sourceTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();
        var audit = new InMemoryAuditSink();
        var guard = CreateGuard(
            principal: new AuthenticatedPrincipal(Guid.NewGuid(), sourceTenant, new[] { "Manager" }, null),
            resolvedTenant: sourceTenant,
            auditSink: audit);

        var result = await guard.EvaluateAsync(new PrivilegedTenantOperationRequest(targetTenant, "op"));

        Assert.False(result.IsAllowed);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("CROSS_TENANT_FORBIDDEN", result.ErrorCode);
        Assert.Single(audit.Events);
        Assert.Equal("rejected", audit.Events[0].Outcome);
    }

    [Fact]
    public async Task EvaluateAsync_WhenCrossTenantAndAdminRole_AllowsAndAudits()
    {
        var sourceTenant = Guid.NewGuid();
        var targetTenant = Guid.NewGuid();
        var audit = new InMemoryAuditSink();
        var guard = CreateGuard(
            principal: new AuthenticatedPrincipal(Guid.NewGuid(), sourceTenant, new[] { "Admin" }, null),
            resolvedTenant: sourceTenant,
            auditSink: audit);

        var result = await guard.EvaluateAsync(new PrivilegedTenantOperationRequest(targetTenant, "op"));

        Assert.True(result.IsAllowed);
        Assert.Single(audit.Events);
        Assert.Equal("allowed", audit.Events[0].Outcome);
    }

    [Fact]
    public async Task EvaluateAsync_WhenPrincipalMissing_RejectsAndAudits()
    {
        var audit = new InMemoryAuditSink();
        var guard = CreateGuard(
            principal: null,
            resolvedTenant: Guid.NewGuid(),
            auditSink: audit);

        var result = await guard.EvaluateAsync(new PrivilegedTenantOperationRequest(Guid.NewGuid(), "op"));

        Assert.False(result.IsAllowed);
        Assert.Equal(401, result.StatusCode);
        Assert.Equal("AUTH_UNAUTHORIZED", result.ErrorCode);
        Assert.Single(audit.Events);
        Assert.Equal("rejected", audit.Events[0].Outcome);
    }

    private static IPrivilegedTenantOperationGuard CreateGuard(
        AuthenticatedPrincipal? principal,
        Guid? resolvedTenant,
        IAuthorizationDecisionAuditSink auditSink)
    {
        return new PrivilegedTenantOperationGuard(
            new StubPrincipalAccessor(principal),
            new StubTenantAccessor(resolvedTenant),
            auditSink);
    }

    private sealed class StubPrincipalAccessor(AuthenticatedPrincipal? principal) : IAuthenticatedPrincipalAccessor
    {
        public AuthenticatedPrincipal? GetCurrent() => principal;
    }

    private sealed class StubTenantAccessor(Guid? tenantId) : ITenantContextAccessor
    {
        public Guid? GetCurrentTenantId() => tenantId;
    }

    private sealed class InMemoryAuditSink : IAuthorizationDecisionAuditSink
    {
        public List<AuthorizationDecisionAuditEvent> Events { get; } = new();

        public Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }
}
