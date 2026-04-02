namespace GTEK.FSM.MobileApp.Tests.Navigation;

using GTEK.FSM.MobileApp.Navigation;
using GTEK.FSM.Shared.Contracts.Vocabulary;

public sealed class RoleGateResolverTests
{
    [Theory]
    [InlineData("worker", MobileSectionVisibility.WorkerOnly)]
    [InlineData("customer", MobileSectionVisibility.CustomerOnly)]
    [InlineData("worker,support", MobileSectionVisibility.WorkerOnly)]
    [InlineData("customer support", MobileSectionVisibility.CustomerOnly)]
    [InlineData("worker;customer", MobileSectionVisibility.Both)]
    [InlineData("", MobileSectionVisibility.Both)]
    public void Resolve_ReturnsExpectedVisibilityForRoles(string rawRole, MobileSectionVisibility expected)
    {
        var visibility = RoleGateResolver.Resolve(rawRole);

        Assert.Equal(expected, visibility);
    }

    [Theory]
    [InlineData("support", true, AuthorizationUxAccessState.Allowed)]
    [InlineData("support", false, AuthorizationUxAccessState.Disabled)]
    [InlineData("customer", true, AuthorizationUxAccessState.Hidden)]
    public void EvaluateActionAccess_ReturnsExpectedState(string rawRole, bool tenantAllowed, AuthorizationUxAccessState expected)
    {
        var accessState = RoleGateResolver.EvaluateActionAccess(rawRole, tenantAllowed, "support", "manager", "admin");

        Assert.Equal(expected, accessState);
    }

    [Theory]
    [InlineData("customer", true, AuthorizationUxPolicy.ForbiddenByRoleMessage)]
    [InlineData("support", false, AuthorizationUxPolicy.ForbiddenByTenantMessage)]
    [InlineData("support", true, "")]
    public void BuildForbiddenFeedback_ReturnsExpectedCopy(string rawRole, bool tenantAllowed, string expected)
    {
        var feedback = RoleGateResolver.BuildForbiddenFeedback(rawRole, tenantAllowed, "support", "manager", "admin");

        Assert.Equal(expected, feedback);
    }
}
