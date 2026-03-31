using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Backend.Application.Subscriptions;

using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationDecisionAuditSink, NoOpAuthorizationDecisionAuditSink>();
        services.AddScoped<IPrivilegedTenantOperationGuard, PrivilegedTenantOperationGuard>();
        services.AddScoped<ITenantOwnershipGuard, TenantOwnershipGuard>();
        services.AddScoped<IServiceRequestCreationService, ServiceRequestCreationService>();
        services.AddScoped<IServiceRequestLifecycleService, ServiceRequestLifecycleService>();
        services.AddScoped<IServiceRequestAssignmentService, ServiceRequestAssignmentService>();
        services.AddScoped<IServiceRequestQueryService, ServiceRequestQueryService>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
        services.AddScoped<ISubscriptionManagementService, SubscriptionManagementService>();

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
