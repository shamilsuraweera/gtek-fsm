using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Categories;
using GTEK.FSM.Backend.Application.Decisioning;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Reporting;
using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Backend.Application.Subscriptions;
using GTEK.FSM.Backend.Application.Workers;

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
        services.AddScoped<IOperationalUpdatePublisher, NoOpOperationalUpdatePublisher>();
        services.AddScoped<IPrivilegedTenantOperationGuard, PrivilegedTenantOperationGuard>();
        services.AddScoped<ITenantOwnershipGuard, TenantOwnershipGuard>();
        services.AddScoped<IServiceRequestCreationService, ServiceRequestCreationService>();
        services.AddScoped<IServiceRequestLifecycleService, ServiceRequestLifecycleService>();
        services.AddScoped<IServiceRequestAssignmentService, ServiceRequestAssignmentService>();
        services.AddScoped<IServiceRequestQueryService, ServiceRequestQueryService>();
        services.AddScoped<IJobQueryService, JobQueryService>();
        services.AddScoped<IAuditLogQueryService, AuditLogQueryService>();
        services.AddScoped<IManagementReportingQueryService, ManagementReportingQueryService>();
        services.AddScoped<ICategoryQueryService, CategoryQueryService>();
        services.AddScoped<ICategoryManagementService, CategoryManagementService>();
        services.AddScoped<ISubscriptionQueryService, SubscriptionQueryService>();
        services.AddScoped<ISubscriptionManagementService, SubscriptionManagementService>();
        services.AddScoped<IWorkerQueryService, WorkerQueryService>();
        services.AddScoped<IWorkerManagementService, WorkerManagementService>();
        services.AddScoped<IWorkerMatchingService, WorkerMatchingService>();
        services.AddSingleton<IDecisioningMetricsCollector, InMemoryDecisioningMetricsCollector>();

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

    private sealed class NoOpOperationalUpdatePublisher : IOperationalUpdatePublisher
    {
        public Task PublishServiceRequestStatusUpdatedAsync(TransitionedServiceRequestPayload payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishJobAssignmentUpdatedAsync(AssignedServiceRequestPayload payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task PublishSlaEscalationTriggeredAsync(SlaEscalationTriggeredPayload payload, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
