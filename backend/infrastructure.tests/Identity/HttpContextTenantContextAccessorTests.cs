using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Infrastructure.Identity;

using Microsoft.AspNetCore.Http;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class HttpContextTenantContextAccessorTests
{
    [Fact]
    public void GetCurrentTenantId_WithGuidItem_ReturnsTenantId()
    {
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext();
        context.Items[TenantContextConstants.HttpContextItemKey] = tenantId;

        var accessor = CreateAccessor(context);
        var result = accessor.GetCurrentTenantId();

        Assert.Equal(tenantId, result);
    }

    [Fact]
    public void GetCurrentTenantId_WithMissingItem_ReturnsNull()
    {
        var accessor = CreateAccessor(new DefaultHttpContext());

        var result = accessor.GetCurrentTenantId();

        Assert.Null(result);
    }

    [Fact]
    public void GetCurrentTenantId_WithMalformedStringItem_ReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Items[TenantContextConstants.HttpContextItemKey] = "invalid";

        var accessor = CreateAccessor(context);
        var result = accessor.GetCurrentTenantId();

        Assert.Null(result);
    }

    private static HttpContextTenantContextAccessor CreateAccessor(HttpContext context)
    {
        return new HttpContextTenantContextAccessor(new HttpContextAccessor { HttpContext = context });
    }
}
