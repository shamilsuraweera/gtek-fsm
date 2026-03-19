using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Http;

namespace GTEK.FSM.Backend.Infrastructure.Identity;

public sealed class HttpContextTenantContextAccessor(IHttpContextAccessor httpContextAccessor) : ITenantContextAccessor
{
    public Guid? GetCurrentTenantId()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null)
        {
            return null;
        }

        if (!context.Items.TryGetValue(TenantContextConstants.HttpContextItemKey, out var rawValue))
        {
            return null;
        }

        return rawValue switch
        {
            Guid tenantId => tenantId,
            string text when Guid.TryParse(text, out var parsed) => parsed,
            _ => null
        };
    }
}
