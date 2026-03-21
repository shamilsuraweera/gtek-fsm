# SQL Server Connection Strategy

## Overview

This document outlines how SQL Server database connections are configured and managed across different deployment environments. The strategy ensures secure, environment-aware connection management with zero hardcoded credentials and support for local development, staging, and production deployments.

---

## Architecture Overview

```text
┌─────────────────────────────────────────────────────┐
│  Environment Configuration Layer (appsettings)     │
│  - Base: appsettings.json (placeholder)             │
│  - Overrides: appsettings.{Environment}.json        │
│  - Runtime: Environment Variables (highest priority)│
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│  DatabaseOptions Class (DI-Registered)             │
│  - Binds to Configuration["Database"]               │
│  - Provides GetConnectionString() helper            │
│  - Supports database name override (multi-tenancy)  │
└──────────────────┬──────────────────────────────────┘
                   │
┌──────────────────▼──────────────────────────────────┐
│  Entity Framework Core (Future Phase 1)             │
│  - DbContext registration (defer to Phase 1)        │
│  - Uses DatabaseOptions.ConnectionString            │
│  - Supports migration infrastructure                │
└─────────────────────────────────────────────────────┘
```

---

## Environment-Specific Configurations

### Local Development (Windows/Ubuntu)

**File:** `appsettings.Development.json`

**Connection String:**

```text
Server=.;Database=GTEK_FSM_Local;Integrated Security=true;Encrypt=false;TrustServerCertificate=true;
```

**Requirements:**

- SQL Server Express or Developer Edition installed locally
- Windows Authentication enabled (local domain membership)
- Database name: `GTEK_FSM_Local`
- No firewall restrictions for local connections

**Setup:**

````powershell
# Windows: Create database
sqlcmd -S . -Q "CREATE DATABASE GTEK_FSM_Local;"

# Ubuntu (via Docker or remote SQL Server):
# Use Docker container or SQL Server on Linux instance
# Update connection string to remote server address
```text

### Development / Staging Environment

**File:** `appsettings.Development.json` (if using shared dev server) or environment variables

**Connection String Pattern:**

````

Server=localhost,1433;Database=GTEK_FSM_Dev;User Id=sa;Password=<STRONG_PASSWORD>;Encrypt=false;TrustServerCertificate=true;

````text

**Requirements:**

- SQL Server running in Docker container or VM
- SQL Server Authentication (sa account or dedicated user)
- Port 1433 exposed and accessible
- Network connectivity from API to database server

**Setup:**

```bash
# Docker: Start SQL Server container
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=<STRONG_PASSWORD>" \
  -p 1433:1433 --name gtek-fsm-sql \
  mcr.microsoft.com/mssql/server:2022-latest

# Create database
sqlcmd -S localhost,1433 -U sa -P <PASSWORD> \
  -Q "CREATE DATABASE GTEK_FSM_Dev;"
````

### Production Environment

**Configuration Source:** Environment Variables or Azure Key Vault (NOT in appsettings)

**Connection String Pattern:**

```text
Server=<PROD_SQL_SERVER>.<REGION>.database.windows.net;Database=GTEK_FSM_Prod;User Id=<PROD_USER>;Password=<PROD_PASSWORD>;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;
```

**Requirements:**

- Azure SQL Database, AWS RDS, or managed SQL Server instance
- SQL Server Authentication (username/password or Azure AD)
- Encrypted connections (mandatory)
- Private network or VPN access only
- Connection pooling enabled
- Firewall rules restricting access to API servers only

**Setup:**

````bash
# Set environment variable (CI/CD pipeline or server configuration)
export DATABASE_CONNECTION_STRING="Server=prod-server;Database=GTEK_FSM_Prod;User Id=...;Password=...;Encrypt=true;"

# Or use Azure Key Vault (recommended)
# Reference: https://learn.microsoft.com/en-us/azure/key-vault/general/overview
```text

---

## Connection String Configuration Priority

The configuration system loads values in this order (highest to lowest priority):

1. **Environment Variables** — `DATABASE__CONNECTIONSTRING` or `Database__ConnectionString`
   - Override any appsettings value
   - Used in containerized and cloud deployments
   - Recommended for secrets (never commit to source control)

2. **Environment-Specific appsettings** — `appsettings.{Environment}.json`
   - Applied on top of base appsettings
   - Values override base configuration
   - Examples: Development, Staging, Production

3. **Base appsettings** — `appsettings.json`
   - Fallback defaults
   - Kept as placeholders (empty strings)
   - Non-sensitive infrastructure documentation

---

## DatabaseOptions Class

The `DatabaseOptions` configuration class binds to the `"Database"` section in appsettings:

```csharp
public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string GetConnectionString(string? databaseName = null)
    {
        // Override database name if provided (multi-tenancy support)
        if (string.IsNullOrEmpty(databaseName))
            return ConnectionString;

        var builder = new SqlConnectionStringBuilder(ConnectionString)
        {
            InitialCatalog = databaseName
        };
        return builder.ConnectionString;
    }
}
````

**Registered in DI (Program.cs):**

````csharp
services.Configure<DatabaseOptions>(configuration.GetSection("Database"));
```text

**Consumed in services:**

```csharp
public class MyRepository
{
    private readonly IOptions<DatabaseOptions> _dbOptions;

    public MyRepository(IOptions<DatabaseOptions> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    public void Connect()
    {
        using var connection = new SqlConnection(_dbOptions.Value.ConnectionString);
        // Use connection
    }
}
````

---

## Multi-Tenancy Support

The `GetConnectionString(databaseName)` method enables per-tenant database isolation:

````csharp
// Get connection string for a specific tenant database
string tenantDb = $"GTEK_FSM_Tenant_{tenantId}";
string tenantConnectionString = _dbOptions.Value.GetConnectionString(tenantDb);
```text

**Tenant Database Naming Convention:**

- Local: `GTEK_FSM_Local_Tenant_{TenantId}`
- Development: `GTEK_FSM_Dev_Tenant_{TenantId}`
- Production: `GTEK_FSM_Prod_Tenant_{TenantId}`

---

## Security Best Practices

### 1. Never Commit Secrets

```bash
# Good: Secrets in environment variables
export DATABASE__CONNECTIONSTRING="Server=...;Password=***;"

# Bad: Secrets in appsettings files (NEVER DO THIS)
# {
#   "Database": {
#     "ConnectionString": "Server=...;Password=actual_password;"
#   }
# }
````

### 2. Use User Secrets in Development

````bash
# Store local development password securely
dotnet user-secrets init
dotnet user-secrets set "Database:ConnectionString" "Server=.;Database=GTEK_FSM_Local;..."
```text

### 3. Encrypt Connections in Production

```csharp
// Production connection string MUST use Encrypt=true
"Server=prod-server;Encrypt=true;TrustServerCertificate=false;"
````

### 4. Use Managed Identity (Cloud)

For Azure deployments, prefer managed identity over username/password:

````csharp
// Azure SQL with Managed Identity
"Server=tcp:myserver.database.windows.net,1433;Initial Catalog=mydb;Encrypt=true;TrustServerCertificate=false;Connection Timeout=30;Authentication=Active Directory Default;"
```text

### 5. Rotate Credentials Regularly

- Define rotation policy (e.g., quarterly)
- Update environment variables and secrets vault
- Monitor failed connection attempts

---

## Connection Pooling & Tuning

**Recommended Connection String Parameters:**

````

;Min Pool Size=5;Max Pool Size=100;Connection Lifetime=300;Connection Idle Timeout=5;Pooling=true;

````text

**Explanation:**

- `Min Pool Size=5` — Maintain 5 open connections in pool
- `Max Pool Size=100` — Allow up to 100 concurrent connections
- `Connection Lifetime=300` — Recycle connections after 5 minutes
- `Connection Idle Timeout=5` — Close idle connections after 5 seconds
- `Pooling=true` — Enable connection pooling (improves performance)

---

## Environment Variable Reference

### For Docker / Kubernetes

```bash
# Set in container environment or deployment manifest
DATABASE__CONNECTIONSTRING=Server=sql-service;Database=GTEK_FSM_Prod;User Id=sa;Password=***;Encrypt=true;
````

### For CI/CD Pipeline (GitHub Actions, Azure Pipelines)

````yaml
env:
  DATABASE__CONNECTIONSTRING: ${{ secrets.DATABASE_CONNECTION_STRING }}
```text

### For Local Development (User Secrets)

```bash
dotnet user-secrets set "Database:ConnectionString" "Server=.;Database=GTEK_FSM_Local;Integrated Security=true;"
````

---

## Troubleshooting Connection Issues

### Issue: "Cannot connect to server"

**Possible Causes:**

- SQL Server not running
- Firewall blocking access
- Incorrect server address/port
- Network connectivity issue

**Solution:**

````bash
# Test connectivity
sqlcmd -S <SERVER> -U <USER> -P <PASSWORD> -Q "SELECT 1"

# Or from .NET
var connection = new SqlConnection(connectionString);
connection.Open(); // Will throw if connection fails
```text

### Issue: "Login failed for user"

**Possible Causes:**

- Incorrect username/password
- User account disabled in SQL Server
- Wrong authentication mode

**Solution:**

```bash
# Verify credentials in SQL Server Management Studio
# Create new user if needed: CREATE LOGIN [username] WITH PASSWORD = '***'
````

### Issue: "Timeout expired"

**Possible Causes:**

- Server under heavy load
- Network latency
- Firewall timeout

**Solution:**

````csharp
// Increase timeout in connection string
"Server=...;Connection Timeout=60;" // 60 seconds instead of default 15
```text

---

## Migration Strategy (Phase 0.7.2)

This task sets up the configuration layer only. Database schema migration strategy will be defined in 0.7.2, including:

- EF Core migration project structure
- Seed data strategy
- Migration versioning and rollback procedures
- Local development database initialization

Currently, `DatabaseOptions` is registered in DI but **no DbContext is configured**. DbContext registration will follow in Phase 1 when domain models are defined.

---

## Related Tasks

- **0.7.2:** Database migration strategy skeleton
- **0.7.3:** Seed data and local startup procedures
- **0.7.4:** Containerization (Docker Compose with SQL Server)
- **1.x.x:** Entity Framework Core DbContext and data access layer (Phase 1)

---

## Summary

| Aspect | Configuration |
| -------- | --------------- |
| **Base Config** | `appsettings.json` (placeholder) |
| **Local Dev** | Windows Auth, `GTEK_FSM_Local` database |
| **Dev/Staging** | SQL Auth, Docker container or remote server |
| **Production** | Environment variables or Key Vault, encrypted, managed identity |
| **Secrets** | Never in source control; use env vars or user secrets |
| **Multi-Tenancy** | `GetConnectionString(tenantName)` helper method |
| **Pooling** | Enabled by default with recommended Min=5, Max=100 |
````
