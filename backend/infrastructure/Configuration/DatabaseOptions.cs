using Microsoft.Data.SqlClient;

namespace GTEK.FSM.Backend.Infrastructure.Configuration;

/// <summary>
/// SQL Server database configuration options bound from appsettings.
/// 
/// Usage in appsettings.json:
/// {
///   "Database": {
///     "ConnectionString": "Server=...;Database=...;"
///   }
/// }
/// 
/// Registered in DI as:
/// services.Configure&lt;DatabaseOptions&gt;(configuration.GetSection("Database"));
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// SQL Server connection string for the default database context.
    /// Environment-specific values come from appsettings.{Environment}.json overrides.
    /// 
    /// Local (Windows Auth): Server=.;Database=GTEK_FSM_Local;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;
    /// Development (SQL Auth): Server=localhost,1433;Database=GTEK_FSM_Dev;User Id=sa;Password=***;Encrypt=false;TrustServerCertificate=true;
    /// Production: Set via environment variable or secure vault (e.g., Azure Key Vault)
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets the connection string with optional database name override for testing or multi-tenancy scenarios.
    /// If databaseName is provided, replaces Database= parameter in connection string.
    /// </summary>
    public string GetConnectionString(string? databaseName = null)
    {
        if (string.IsNullOrEmpty(databaseName))
        {
            return ConnectionString;
        }

        // Replace the Database parameter in connection string
        var connectionStringBuilder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = databaseName
        };

        return connectionStringBuilder.ConnectionString;
    }
}
