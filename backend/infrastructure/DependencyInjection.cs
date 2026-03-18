using GTEK.FSM.Backend.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GTEK.FSM.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Register DatabaseOptions from configuration
        services.Configure<DatabaseOptions>(configuration.GetSection("Database"));

        // SQL Server provider registration (DbContext will be added in Phase 1)
        // Future: services.AddDbContext<ApplicationDbContext>(...);

        return services;
    }
}
