using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Authorization;

namespace GTEK.FSM.Backend.Api.Authorization;

public static class AuthorizationServiceCollectionExtensions
{
    public static IServiceCollection AddApiAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorization(options =>
        {
            foreach (var policyPermission in AuthorizationPolicyCatalog.GetPolicyPermissions())
            {
                options.AddPolicy(policyPermission.Key, policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.Requirements.Add(new PermissionRequirement(policyPermission.Value));
                });
            }
        });

        return services;
    }
}
