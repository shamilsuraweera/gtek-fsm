namespace GTEK.FSM.WebPortal.Tests.Component;

using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Services.Security;

public sealed class UiSecurityContextAuthorizationUxTests
{
    private readonly UiSecurityContext context = new();

    [Fact]
    public void GetActionAccessState_ReturnsAllowed_WhenRoleAndTenantMatch()
    {
        var accessState = this.context.GetActionAccessState("TENANT-01", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Allowed, accessState);
    }

    [Fact]
    public void GetActionAccessState_ReturnsHidden_WhenRoleNotAllowed()
    {
        var accessState = this.context.GetActionAccessState("TENANT-01", PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Hidden, accessState);
    }

    [Fact]
    public void GetActionAccessState_ReturnsDisabled_WhenTenantNotAccessible()
    {
        var accessState = this.context.GetActionAccessState("TENANT-99", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Disabled, accessState);
    }

    [Fact]
    public void GetForbiddenFeedback_ReturnsSharedTenantCopy()
    {
        var feedback = this.context.GetForbiddenFeedback("TENANT-99", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxPolicy.ForbiddenByTenantMessage, feedback);
    }
}