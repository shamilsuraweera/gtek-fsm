using GTEK.FSM.Backend.Application.Identity;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class RolePermissionAuthorizerTests
{
    [Fact]
    public void IsAuthorizedForPermission_WhenRoleMatchesPermission_ReturnsTrue()
    {
        var result = RolePermissionAuthorizer.IsAuthorizedForPermission(
            roles: new[] { "Support" },
            permission: Permissions.ServiceRequestsWrite);

        Assert.True(result);
    }

    [Fact]
    public void IsAuthorizedForPermission_WhenNoRoleMatchesPermission_ReturnsFalse()
    {
        var result = RolePermissionAuthorizer.IsAuthorizedForPermission(
            roles: new[] { "Customer" },
            permission: Permissions.UsersWrite);

        Assert.False(result);
    }

    [Fact]
    public void IsAuthorizedForPermission_WhenUnknownRolePresent_IgnoresUnknownRole()
    {
        var result = RolePermissionAuthorizer.IsAuthorizedForPermission(
            roles: new[] { "UnknownRole", "Admin" },
            permission: Permissions.TenantsWrite);

        Assert.True(result);
    }
}
