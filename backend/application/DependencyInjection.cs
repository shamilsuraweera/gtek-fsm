using GTEK.FSM.Backend.Application.Identity;

using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationDecisionAuditSink, NoOpAuthorizationDecisionAuditSink>();
        services.AddScoped<IPrivilegedTenantOperationGuard, PrivilegedTenantOperationGuard>();
        services.AddScoped<ITenantOwnershipGuard, TenantOwnershipGuard>();

        return services;
    }

    private sealed class NoOpAuthorizationDecisionAuditSink : IAuthorizationDecisionAuditSink
    {
        public Task WriteAsync(AuthorizationDecisionAuditEvent auditEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
