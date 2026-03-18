using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GTEK.FSM.Backend.Infrastructure.Persistence.DesignTime;

/// <summary>
/// Design-time factory used by dotnet-ef to create DbContext instances.
/// </summary>
public class GtekFsmDbContextFactory : IDesignTimeDbContextFactory<GTEK.FSM.Backend.Infrastructure.Persistence.GtekFsmDbContext>
{
    public GTEK.FSM.Backend.Infrastructure.Persistence.GtekFsmDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var apiConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "../api");
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(apiConfigPath, "appsettings.json"), optional: false)
            .AddJsonFile(Path.Combine(apiConfigPath, $"appsettings.{environment}.json"), optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration["Database:ConnectionString"]
            ?? configuration.GetConnectionString("MainDb")
            ?? "Server=.;Database=GTEK_FSM_Local;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;";

        var optionsBuilder = new DbContextOptionsBuilder<GTEK.FSM.Backend.Infrastructure.Persistence.GtekFsmDbContext>();
        optionsBuilder.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(GTEK.FSM.Backend.Infrastructure.Persistence.GtekFsmDbContext).Assembly.FullName);
            sql.MigrationsHistoryTable("__EFMigrationsHistory", "dbo");
        });

        return new GTEK.FSM.Backend.Infrastructure.Persistence.GtekFsmDbContext(optionsBuilder.Options);
    }
}
