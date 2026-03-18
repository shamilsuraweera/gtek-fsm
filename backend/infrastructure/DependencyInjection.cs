using GTEK.FSM.Backend.Infrastructure.Configuration;
using GTEK.FSM.Backend.Infrastructure.Persistence;
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

        return services;
    }
}
