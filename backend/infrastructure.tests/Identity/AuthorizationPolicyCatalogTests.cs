using GTEK.FSM.Backend.Application.Identity;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class AuthorizationPolicyCatalogTests
{
    [Fact]
    public void GetPolicyPermissions_ContainsAllPhase2RoleFlowPolicies()
    {
        var map = AuthorizationPolicyCatalog.GetPolicyPermissions();

        Assert.Equal(Permissions.ServiceRequestsWrite, map[AuthorizationPolicyCatalog.CustomerFlow]);
        Assert.Equal(Permissions.JobsWrite, map[AuthorizationPolicyCatalog.WorkerFlow]);
        Assert.Equal(Permissions.ServiceRequestsWrite, map[AuthorizationPolicyCatalog.SupportFlow]);
        Assert.Equal(Permissions.UsersWrite, map[AuthorizationPolicyCatalog.ManagementFlow]);
        Assert.Equal(Permissions.TenantsWrite, map[AuthorizationPolicyCatalog.AdminFlow]);
    }

    [Fact]
    public void GetPolicyPermissions_IncludesSystemPingPolicy()
    {
        var map = AuthorizationPolicyCatalog.GetPolicyPermissions();

        Assert.Equal(Permissions.SystemPing, map[AuthorizationPolicyCatalog.SystemPing]);
    }
}
