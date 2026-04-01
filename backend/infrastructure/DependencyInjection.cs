using GTEK.FSM.Backend.Infrastructure.Configuration;
using GTEK.FSM.Backend.Infrastructure.Identity;
using GTEK.FSM.Backend.Infrastructure.Persistence;
using GTEK.FSM.Backend.Infrastructure.Persistence.Repositories;
using GTEK.FSM.Backend.Infrastructure.Persistence.Transactions;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
        services.Configure<SignalROptions>(configuration.GetSection("SignalR"));
        services.Configure<StorageOptions>(configuration.GetSection("Storage"));
        services.Configure<ExternalServicesOptions>(configuration.GetSection("ExternalServices"));

        var connectionString = configuration["Database:ConnectionString"]
            ?? configuration.GetConnectionString("MainDb")
            ?? string.Empty;

        // Register EF Core context for migration infrastructure.
        // Domain entity sets and mappings will be added in Phase 1.
        services.AddDbContext<GtekFsmDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(GtekFsmDbContext).Assembly.FullName);
                sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
            }));

        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
        services.AddScoped<IJobRepository, JobRepository>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IWorkerProfileRepository, WorkerProfileRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddHttpContextAccessor();
        services.AddScoped<IAuthorizationDecisionAuditSink, AuthorizationDecisionAuditLogger>();
        services.AddScoped<IAuthenticatedPrincipalAccessor, HttpContextAuthenticatedPrincipalAccessor>();
        services.AddScoped<ITenantContextAccessor, HttpContextTenantContextAccessor>();

        // Register audit log writer
        services.AddScoped<GTEK.FSM.Backend.Application.Audit.IAuditLogWriter, GTEK.FSM.Backend.Infrastructure.Audit.EfAuditLogWriter>();

        return services;
    }
}
