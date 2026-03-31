using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Backend.Application.Subscriptions;

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IAuthorizationDecisionAuditSink, NoOpAuthorizationDecisionAuditSink>();
        services.AddScoped<IAuditLogWriter, NoOpAuditLogWriter>();
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

    private sealed class NoOpAuditLogWriter : IAuditLogWriter
    {
        public Task WriteAsync(GTEK.FSM.Backend.Domain.Audit.AuditLog log, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
