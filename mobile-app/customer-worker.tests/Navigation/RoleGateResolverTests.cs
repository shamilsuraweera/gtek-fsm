namespace GTEK.FSM.MobileApp.Tests.Navigation;

using GTEK.FSM.MobileApp.Navigation;

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
}
