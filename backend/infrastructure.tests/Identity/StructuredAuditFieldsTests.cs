using GTEK.FSM.Backend.Application.Identity;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

/// <summary>
/// Unit tests verifying that structured audit logging fields (`TenantId`, `UserId`, `Action`, `Outcome`)
/// are properly captured and preserved in authorization decision audit events.
/// 
/// These tests verify that the audit infrastructure supports all required structured fields
/// for compliance, troubleshooting, and security monitoring of identity and authorization decisions.
/// </summary>
public sealed class StructuredAuditFieldsTests
{
    [Fact]
    public void AuthorizationDecisionAuditEvent_StoresAllStructuredFields()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var sourceTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();
        var action = "permission_check:TenantsWrite";
        var outcome = "allowed";
        var reason = "permission_granted";
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: userId,
            SourceTenantId: sourceTenantId,
            TargetTenantId: targetTenantId,
            Action: action,
            Outcome: outcome,
            Reason: reason,
            OccurredAtUtc: timestamp);

        // Assert - Verify all structured fields are present
        Assert.Equal(userId, auditEvent.UserId);
        Assert.Equal(sourceTenantId, auditEvent.SourceTenantId);
        Assert.Equal(targetTenantId, auditEvent.TargetTenantId);
        Assert.Equal(action, auditEvent.Action);
        Assert.Equal(outcome, auditEvent.Outcome);
        Assert.Equal(reason, auditEvent.Reason);
        Assert.Equal(timestamp, auditEvent.OccurredAtUtc);
    }

    [Fact]
    public void AuthorizationDecisionAuditEvent_SupportsSameSourceAndTargetTenant()
    {
        // Arrange - same-tenant operation
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: userId,
            SourceTenantId: tenantId,
            TargetTenantId: tenantId,
            Action: "list_users",
            Outcome: "allowed",
            Reason: "same_tenant_operation_allowed",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(tenantId, auditEvent.SourceTenantId);
        Assert.Equal(tenantId, auditEvent.TargetTenantId);
        Assert.Equal(tenantId, auditEvent.SourceTenantId);
    }

    [Fact]
    public void AuthorizationDecisionAuditEvent_SupportsDifferentSourceAndTargetTenantForCrossTenant()
    {
        // Arrange - cross-tenant privileged operation
        var userId = Guid.NewGuid();
        var sourceTenantId = Guid.NewGuid();
        var targetTenantId = Guid.NewGuid();

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: userId,
            SourceTenantId: sourceTenantId,
            TargetTenantId: targetTenantId,
            Action: "cross_tenant_operation:modify_tenant",
            Outcome: "allowed",
            Reason: "privileged_cross_tenant_operation_allowed",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert - verify distinct tenants are tracked
        Assert.NotEqual(sourceTenantId, targetTenantId);
        Assert.Equal(sourceTenantId, auditEvent.SourceTenantId);
        Assert.Equal(targetTenantId, auditEvent.TargetTenantId);
    }

    [Fact]
    public void AuthorizationDecisionAuditEvent_SupportsNullUserIdForUnauthenticatedRequests()
    {
        // Arrange - unauthenticated request (no UserId)
        var tenantId = Guid.NewGuid();

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: null,
            SourceTenantId: tenantId,
            TargetTenantId: tenantId,
            Action: "list_public_items",
            Outcome: "rejected",
            Reason: "authentication_required",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert
        Assert.Null(auditEvent.UserId);
        Assert.Equal(tenantId, auditEvent.SourceTenantId);
        Assert.Equal("rejected", auditEvent.Outcome);
    }

    [Fact]
    public void AuthorizationDecisionAuditEvent_CapturesDeniedOutcome()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: userId,
            SourceTenantId: tenantId,
            TargetTenantId: tenantId,
            Action: "permission_check:TenantsWrite",
            Outcome: "denied",
            Reason: "permission_insufficient",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal("denied", auditEvent.Outcome);
        Assert.Equal("permission_insufficient", auditEvent.Reason);
    }

    [Fact]
    public void AuthorizationDecisionAuditEvent_IncludesTimestampInUtc()
    {
        // Arrange
        var beforeCreate = DateTimeOffset.UtcNow;

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: Guid.NewGuid(),
            SourceTenantId: Guid.NewGuid(),
            TargetTenantId: null,
            Action: "test",
            Outcome: "allowed",
            Reason: "test_reason",
            OccurredAtUtc: beforeCreate);

        var afterCreate = DateTimeOffset.UtcNow;

        // Assert - timestamp is within expected range
        Assert.InRange(auditEvent.OccurredAtUtc, beforeCreate.AddSeconds(-1), afterCreate.AddSeconds(1));
        Assert.Equal(DateTimeKind.Utc, auditEvent.OccurredAtUtc.Kind);
    }

    [Theory]
    [InlineData("allowed")]
    [InlineData("denied")]
    [InlineData("rejected")]
    public void AuthorizationDecisionAuditEvent_SupportsMultipleOutcomeValues(string outcome)
    {
        // Arrange
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: userId,
            SourceTenantId: tenantId,
            TargetTenantId: tenantId,
            Action: "test_action",
            Outcome: outcome,
            Reason: $"reason_for_{outcome}",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert
        Assert.Equal(outcome, auditEvent.Outcome);
    }

    [Theory]
    [InlineData("permission_check")]
    [InlineData("cross_tenant_operation")]
    [InlineData("authentication_required")]
    [InlineData("tenant_context_unresolved")]
    public void AuthorizationDecisionAuditEvent_SupportsMultipleActionTypes(string actionPrefix)
    {
        // Arrange
        var fullAction = $"{actionPrefix}:some_permission";

        // Act
        var auditEvent = new AuthorizationDecisionAuditEvent(
            UserId: Guid.NewGuid(),
            SourceTenantId: Guid.NewGuid(),
            TargetTenantId: null,
            Action: fullAction,
            Outcome: "allowed",
            Reason: "test",
            OccurredAtUtc: DateTimeOffset.UtcNow);

        // Assert
        Assert.Contains(actionPrefix, auditEvent.Action);
    }
}
