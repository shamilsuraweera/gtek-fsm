using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class RolePermissionMatrixTests
{
    [Fact]
    public void Guest_HasOnlyPingPermission()
    {
        var permissions = RolePermissionMatrix.GetPermissions(UserRole.Guest);

        Assert.Single(permissions);
        Assert.Contains(Permissions.SystemPing, permissions);
    }

    [Fact]
    public void Customer_CanManageRequests_ButCannotWriteUsers()
    {
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Customer, Permissions.RealTimeConnect));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Customer, Permissions.ServiceRequestsRead));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Customer, Permissions.ServiceRequestsWrite));
        Assert.False(RolePermissionMatrix.HasPermission(UserRole.Customer, Permissions.UsersWrite));
    }

    [Fact]
    public void Worker_CanUpdateJobs_ButCannotWriteSubscriptions()
    {
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Worker, Permissions.RealTimeConnect));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Worker, Permissions.JobsWrite));
        Assert.False(RolePermissionMatrix.HasPermission(UserRole.Worker, Permissions.SubscriptionsWrite));
    }

    [Fact]
    public void Manager_HasBroaderPermissionsThanSupport()
    {
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Support, Permissions.RealTimeConnect));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Manager, Permissions.RealTimeConnect));
        Assert.False(RolePermissionMatrix.HasPermission(UserRole.Support, Permissions.UsersWrite));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Manager, Permissions.UsersWrite));

        Assert.False(RolePermissionMatrix.HasPermission(UserRole.Support, Permissions.SubscriptionsWrite));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Manager, Permissions.SubscriptionsWrite));
    }

    [Fact]
    public void Admin_HasTenantWritePermission()
    {
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Admin, Permissions.RealTimeConnect));
        Assert.True(RolePermissionMatrix.HasPermission(UserRole.Admin, Permissions.TenantsWrite));
    }
}
