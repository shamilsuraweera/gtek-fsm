using GTEK.FSM.Backend.Application.Identity;

using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ITenantOwnershipGuard, TenantOwnershipGuard>();

        return services;
    }
}
