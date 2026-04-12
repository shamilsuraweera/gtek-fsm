using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Automation;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Backend.Domain.Aggregates;
using GTEK.FSM.Backend.Domain.Audit;
using GTEK.FSM.Backend.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Runtime;

public class OperationalAutomationServiceRuntimeTests
{
    [Fact]
    public async Task ExecuteAsync_WhenSlaReminderAlreadyRecordedWithinCooldown_SkipsDuplicateAction()
    {
        var tenant = new Tenant(Guid.NewGuid(), "tenant-a", "Tenant A");
        var request = new ServiceRequest(Guid.NewGuid(), tenant.Id, Guid.NewGuid(), "HVAC issue");
        request.ApplySlaSnapshot(
            responseDueAtUtc: DateTime.UtcNow.AddHours(2),
            assignmentDueAtUtc: null,
            completionDueAtUtc: null,
            responseSlaState: SlaState.AtRisk,
            assignmentSlaState: SlaState.NotApplicable,
            completionSlaState: SlaState.NotApplicable,
            nextSlaDeadlineAtUtc: DateTime.UtcNow.AddHours(2));

        var auditStore = new InMemoryAutomationAuditStore();
        var provider = BuildProvider(
            tenants: [tenant],
            serviceRequests: [request],
            subscriptions: [],
            auditStore: auditStore);

        var settings = new OperationalAutomationSettings
        {
            Enabled = true,
            ReminderCooldownHours = 24,
            MaxActionsPerTenantPerRun = 5,
        };

        var service = provider.GetRequiredService<IOperationalAutomationService>();
        var firstRun = await service.ExecuteAsync(settings, DateTimeOffset.UtcNow);
        var secondRun = await service.ExecuteAsync(settings, DateTimeOffset.UtcNow.AddHours(1));

        Assert.Equal(1, firstRun.ExecutedCount);
        _ = Assert.Single(auditStore.Items);
        Assert.Equal(0, secondRun.ExecutedCount);
        Assert.Equal(1, secondRun.SkippedCount);
        Assert.Single(auditStore.Items);
        Assert.Equal("Automation:SlaReminder:Response:AtRisk", auditStore.Items[0].Action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTenantBudgetIsOne_OnlyExecutesFirstEligibleRule()
    {
        var tenant = new Tenant(Guid.NewGuid(), "tenant-a", "Tenant A");
        var request = new ServiceRequest(Guid.NewGuid(), tenant.Id, Guid.NewGuid(), "Power outage");
        request.ApplySlaSnapshot(
            responseDueAtUtc: DateTime.UtcNow.AddHours(1),
            assignmentDueAtUtc: null,
            completionDueAtUtc: null,
            responseSlaState: SlaState.Breached,
            assignmentSlaState: SlaState.NotApplicable,
            completionSlaState: SlaState.NotApplicable,
            nextSlaDeadlineAtUtc: DateTime.UtcNow.AddHours(1));

        var subscription = new Subscription(Guid.NewGuid(), tenant.Id, "PRO", DateTime.UtcNow.AddDays(-10), DateTime.UtcNow.AddDays(3));
        var auditStore = new InMemoryAutomationAuditStore();
        var provider = BuildProvider(
            tenants: [tenant],
            serviceRequests: [request],
            subscriptions: [subscription],
            auditStore: auditStore);

        var settings = new OperationalAutomationSettings
        {
            Enabled = true,
            ReminderCooldownHours = 24,
            SubscriptionExpiryReminderDays = 14,
            MaxActionsPerTenantPerRun = 1,
        };

        var service = provider.GetRequiredService<IOperationalAutomationService>();
        var result = await service.ExecuteAsync(settings, DateTimeOffset.UtcNow);

        Assert.Equal(1, result.ExecutedCount);
        Assert.Single(auditStore.Items);
        Assert.Equal("Automation:SlaReminder:Response:Breached", auditStore.Items[0].Action);
    }

    [Fact]
    public async Task ExecuteAsync_WhenSubscriptionIsExpiring_WritesReminderForMatchingTenantOnly()
    {
        var tenantA = new Tenant(Guid.NewGuid(), "tenant-a", "Tenant A");
        var tenantB = new Tenant(Guid.NewGuid(), "tenant-b", "Tenant B");
        var expiringSubscription = new Subscription(Guid.NewGuid(), tenantA.Id, "PRO", DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(5));
        var longLivedSubscription = new Subscription(Guid.NewGuid(), tenantB.Id, "ENTERPRISE", DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(45));
        var auditStore = new InMemoryAutomationAuditStore();
        var provider = BuildProvider(
            tenants: [tenantA, tenantB],
            serviceRequests: [],
            subscriptions: [expiringSubscription, longLivedSubscription],
            auditStore: auditStore);

        var settings = new OperationalAutomationSettings
        {
            Enabled = true,
            EnableSlaReminderWorkflow = false,
            EnableSubscriptionExpiryReminderWorkflow = true,
            ReminderCooldownHours = 24,
            SubscriptionExpiryReminderDays = 14,
            MaxActionsPerTenantPerRun = 5,
        };

        var service = provider.GetRequiredService<IOperationalAutomationService>();
        var result = await service.ExecuteAsync(settings, DateTimeOffset.UtcNow);

        Assert.Equal(1, result.ExecutedCount);
        var audit = Assert.Single(auditStore.Items);
        Assert.Equal(tenantA.Id, audit.TenantId);
        Assert.Equal(expiringSubscription.Id, audit.EntityId);
        Assert.Equal("Automation:SubscriptionExpiryReminder", audit.Action);
    }

    private static ServiceProvider BuildProvider(
        IReadOnlyList<Tenant> tenants,
        IReadOnlyList<ServiceRequest> serviceRequests,
        IReadOnlyList<Subscription> subscriptions,
        InMemoryAutomationAuditStore auditStore)
    {
        var services = new ServiceCollection();
        services.AddApplication();
        services.AddSingleton<ITenantRepository>(new InMemoryTenantRepository(tenants));
        services.AddSingleton<IServiceRequestRepository>(new InMemoryServiceRequestRepository(serviceRequests));
        services.AddSingleton<ISubscriptionRepository>(new InMemorySubscriptionRepository(subscriptions));
        services.AddSingleton<IAuditLogRepository>(auditStore);
        services.AddSingleton<IAuditLogWriter>(auditStore);
        return services.BuildServiceProvider();
    }

    private sealed class InMemoryTenantRepository : ITenantRepository
    {
        private readonly IReadOnlyList<Tenant> tenants;

        public InMemoryTenantRepository(IReadOnlyList<Tenant> tenants)
        {
            this.tenants = tenants;
        }

        public Task AddAsync(Tenant aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
            => Task.FromResult(this.tenants.Any(x => x.Code == tenantCode));

        public Task<Tenant?> GetByCodeAsync(string tenantCode, CancellationToken cancellationToken = default)
            => Task.FromResult(this.tenants.FirstOrDefault(x => x.Code == tenantCode));

        public Task<Tenant?> GetByIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(this.tenants.FirstOrDefault(x => x.Id == tenantId));

        public Task<IReadOnlyList<Tenant>> ListAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(this.tenants);

        public void Remove(Tenant aggregate)
        {
        }

        public void Update(Tenant aggregate)
        {
        }
    }

    private sealed class InMemoryServiceRequestRepository : IServiceRequestRepository
    {
        private readonly IReadOnlyList<ServiceRequest> requests;

        public InMemoryServiceRequestRepository(IReadOnlyList<ServiceRequest> requests)
        {
            this.requests = requests;
        }

        public Task AddAsync(ServiceRequest aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> CountAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
            => Task.FromResult(this.requests.Count(x => x.TenantId == specification.TenantId));

        public Task<ServiceRequest?> GetByIdAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
            => Task.FromResult(this.requests.FirstOrDefault(x => x.TenantId == tenantId && x.Id == requestId));

        public Task<ServiceRequest?> GetForUpdateAsync(Guid tenantId, Guid requestId, CancellationToken cancellationToken = default)
            => this.GetByIdAsync(tenantId, requestId, cancellationToken);

        public Task<IReadOnlyList<ServiceRequest>> ListByCustomerAsync(Guid tenantId, Guid customerUserId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ServiceRequest>>(this.requests.Where(x => x.TenantId == tenantId && x.CustomerUserId == customerUserId).ToArray());

        public Task<IReadOnlyList<ServiceRequest>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ServiceRequest>>(this.requests.Where(x => x.TenantId == tenantId).ToArray());

        public Task<IReadOnlyList<ServiceRequest>> QueryAsync(ServiceRequestQuerySpecification specification, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<ServiceRequest>>(this.requests.Where(x => x.TenantId == specification.TenantId).ToArray());

        public void Remove(ServiceRequest aggregate)
        {
        }

        public void Update(ServiceRequest aggregate)
        {
        }
    }

    private sealed class InMemorySubscriptionRepository : ISubscriptionRepository
    {
        private readonly IReadOnlyList<Subscription> subscriptions;

        public InMemorySubscriptionRepository(IReadOnlyList<Subscription> subscriptions)
        {
            this.subscriptions = subscriptions;
        }

        public Task AddAsync(Subscription aggregate, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<Subscription?> GetActiveByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult(this.subscriptions.FirstOrDefault(x => x.TenantId == tenantId));

        public Task<Subscription?> GetActiveForUpdateByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => this.GetActiveByTenantAsync(tenantId, cancellationToken);

        public Task<Subscription?> GetByIdAsync(Guid tenantId, Guid subscriptionId, CancellationToken cancellationToken = default)
            => Task.FromResult(this.subscriptions.FirstOrDefault(x => x.TenantId == tenantId && x.Id == subscriptionId));

        public Task<IReadOnlyList<Subscription>> ListByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subscription>>(this.subscriptions.Where(x => x.TenantId == tenantId).ToArray());

        public Task<IReadOnlyList<Subscription>> QueryAsync(SubscriptionQuerySpecification specification, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Subscription>>(this.subscriptions.Where(x => x.TenantId == specification.TenantId).ToArray());

        public void Remove(Subscription aggregate)
        {
        }

        public void Update(Subscription aggregate)
        {
        }
    }

    private sealed class InMemoryAutomationAuditStore : IAuditLogRepository, IAuditLogWriter
    {
        public List<AuditLog> Items { get; } = new();

        public Task<int> CountAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.Apply(specification).Count());
        }

        public Task<IReadOnlyList<AuditLog>> QueryAsync(AuditLogQuerySpecification specification, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<AuditLog>>(this.Apply(specification).ToArray());
        }

        public Task WriteAsync(AuditLog log, CancellationToken cancellationToken = default)
        {
            this.Items.Add(log);
            return Task.CompletedTask;
        }

        private IEnumerable<AuditLog> Apply(AuditLogQuerySpecification specification)
        {
            var query = this.Items.Where(x => x.TenantId == specification.TenantId);

            if (specification.EntityId.HasValue)
            {
                query = query.Where(x => x.EntityId == specification.EntityId.Value);
            }

            if (!string.IsNullOrWhiteSpace(specification.EntityType))
            {
                query = query.Where(x => x.EntityType == specification.EntityType);
            }

            if (!string.IsNullOrWhiteSpace(specification.Action))
            {
                query = query.Where(x => x.Action.Contains(specification.Action, StringComparison.Ordinal));
            }

            if (!string.IsNullOrWhiteSpace(specification.Outcome))
            {
                query = query.Where(x => x.Outcome == specification.Outcome);
            }

            if (specification.OccurredFromUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc >= specification.OccurredFromUtc.Value);
            }

            if (specification.OccurredToUtc.HasValue)
            {
                query = query.Where(x => x.OccurredAtUtc <= specification.OccurredToUtc.Value);
            }

            return query;
        }
    }
}