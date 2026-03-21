# GTEK FSM - Configuration & Secrets Management Guide

## Environment Configuration Hierarchy

The application loads configuration in this order (later overrides earlier):

```text
1. appsettings.json (base defaults for all environments)
   ↓
2. appsettings.{ASPNETCORE_ENVIRONMENT}.json (environment-specific)
   ↓
3. Environment Variables (highest priority - secrets management)
```

### Example: Database Connection String

Given `ASPNETCORE_ENVIRONMENT=Development`:

```text
appsettings.json:
{
  "Database": {
    "ConnectionString": ""  // Empty placeholder
  }
}
    ↓
appsettings.Development.json:
{
  "Database": {
    "ConnectionString": "Server=.;Database=GTEK_FSM_Local;..."
  }
}
    ↓
.env (Environment Variables):
Database__ConnectionString=Server=sqlserver;Database=GTEK_FSM_Local;...
    ↓
FINAL VALUE: Server=sqlserver;... (most specific wins)
```

---

## Configuration Keys Reference

### Logging

| Variable                   | Default     | Purpose                                                         |
| -------------------------- | ----------- | --------------------------------------------------------------- |
| `LOGGING_LEVEL`            | Information | Global log level (Trace, Debug, Info, Warning, Error, Critical) |
| `LOGGING_ASPNETCORE_LEVEL` | Warning     | ASP.NET Core framework log level                                |

### Database (SQL Server)

| Variable                     | Default        | Purpose                            |
| ---------------------------- | -------------- | ---------------------------------- |
| `Database__ConnectionString` | Empty          | Full connection string (preferred) |
| `Database__Server`           | . (local)      | SQL Server hostname/IP             |
| `Database__Port`             | 1433           | SQL Server port                    |
| `Database__Database`         | GTEK_FSM_Local | Database name                      |
| `Database__User`             | (Windows Auth) | SQL login username                 |
| `Database__Password`         | (Windows Auth) | SQL login password                 |

**Default (Development):**

```text
Server=.;Database=GTEK_FSM_Local;Integrated Security=true;
```

**Docker (Development):**

```text
Server=sqlserver;Database=GTEK_FSM_Local;User Id=sa;Password=YourStrong!Passw0rd;
```

**Azure SQL (Production):**

```text
Server=gtek-fsm.database.windows.net;Database=GTEK_FSM;
Authentication=Active Directory Managed Identity;
```

### SignalR (Phase 4: Real-time)

| Variable                           | Default        | Purpose                        |
| ---------------------------------- | -------------- | ------------------------------ |
| `SignalR__HubPath`                 | /hubs/pipeline | WebSocket endpoint path        |
| `SignalR__EnableDetailedErrors`    | false          | Include stack traces in errors |
| `SignalR__ClientTimeoutSeconds`    | 30             | Client keep-alive interval     |
| `SignalR__HandshakeTimeoutSeconds` | 15             | Connection timeout             |

### Storage (Phase 4+: File/Blob)

| Variable                          | Default   | Purpose                          |
| --------------------------------- | --------- | -------------------------------- |
| `Storage__DefaultProvider`        | Local     | Provider: Local, Blob, S3        |
| `Storage__Local__RootPath`        | ./storage | Local filesystem root            |
| `Storage__Blob__ConnectionString` | Empty     | Azure Storage account connection |
| `Storage__Blob__ContainerName`    | gtek-fsm  | Blob container name              |
| `Storage__S3__ServiceUrl`         | Empty     | AWS S3 endpoint                  |
| `Storage__S3__BucketName`         | Empty     | S3 bucket name                   |
| `Storage__S3__AccessKey`          | Empty     | AWS access key                   |
| `Storage__S3__SecretKey`          | Empty     | AWS secret key                   |
| `Storage__S3__Region`             | us-east-1 | AWS region                       |

### External Services (Phase 3+)

All services follow pattern:

```text
{Service}__Enabled=false|true
{Service}__Provider=None|ProviderName
{Service}__BaseUrl=https://...
{Service}__ApiKey=secret_key_here
{Service}__WebhookSecret=webhook_secret_here
```

**Notifications:**

```text
ExternalServices__Notifications__Enabled=false
ExternalServices__Notifications__Provider=None
ExternalServices__Notifications__BaseUrl=
ExternalServices__Notifications__ApiKey=
```

**Maps:**

```text
ExternalServices__Maps__Enabled=false
ExternalServices__Maps__Provider=None
ExternalServices__Maps__BaseUrl=
ExternalServices__Maps__ApiKey=
```

**Payments:**

```text
ExternalServices__Payments__Enabled=false
ExternalServices__Payments__Provider=None
ExternalServices__Payments__BaseUrl=
ExternalServices__Payments__ApiKey=
ExternalServices__Payments__WebhookSecret=
```

**Webhooks:**

```text
ExternalServices__Webhooks__SignatureHeader=X-Signature
ExternalServices__Webhooks__SigningSecret=
```

### Feature Flags (Phase 0: All disabled)

| Variable                                   | Default | Purpose                      |
| ------------------------------------------ | ------- | ---------------------------- |
| `Features__RealtimePipeline__Enabled`      | false   | Enable SignalR features      |
| `Features__Storage__Enabled`               | false   | Enable file storage features |
| `Features__ExternalNotifications__Enabled` | false   | Enable notification service  |
| `Features__ExternalMaps__Enabled`          | false   | Enable maps service          |
| `Features__Payments__Enabled`              | false   | Enable payments service      |

---

## Secrets Management

### Phase 0: Local Development

**Store secrets in `.env` (NEVER commit):**

````bash
# .env (local development only)
SA_PASSWORD=YourStrong!Passw0rd
JWT_SECRET_KEY=not_a_real_secret_dev_only
```text

**In `.gitignore`:**

````

.env
.env.local
.env.\*.local

````text

### Phase 2: Identity & Key Vault (Future)

**Azure Key Vault Integration:**

```bash
# Production .env (vault reference)
ASPNETCORE_ENVIRONMENT=Production
KEYVAULT_ENDPOINT=https://gtek-fsm.vault.azure.net/
KEYVAULT_TENANTID=xxxxx-xxxxx-xxxxx-xxxxx
````

Application loads secrets from Key Vault:

````csharp
new ConfigurationBuilder()
    .AddAzureKeyVault(...)  // Loads secrets at startup
    .Build()
```text

### Secrets Hierarchy (Recommended)

1. **Local Dev:** `.env` file (git-ignored)
2. **Docker/Staging:** Docker Secrets or environment variables
3. **Production:** Azure Key Vault (managed identity auth)

**Principle:** Secrets **never** in code or version control.

---

## Environment-Specific Configurations

### Local Development

**File:** `appsettings.Development.json`

```json
{
  "Database": {
    "ConnectionString": "Server=.;Database=GTEK_FSM_Local;Integrated Security=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Features": {
    "RealtimePipeline": {
      "Enabled": false
    }
  }
}
````

**Usage:**

````bash
ASPNETCORE_ENVIRONMENT=Development dotnet run
```text

### Docker (Local Team Development)

**Override via `.env`:**

```bash
ASPNETCORE_ENVIRONMENT=Development
SA_PASSWORD=YourStrong!Passw0rd
SQL_SERVER_HOST=sqlserver  # Docker service name
````

**Connection string in app:**

```text
Server=sqlserver;Database=GTEK_FSM_Local;User Id=sa;
Password=YourStrong!Passw0rd;Encrypt=false;
```

### Production (Phase 11 Remote Deployment)

**File:** `appsettings.Production.json`

````json
{
  "Database": {
    "ConnectionString": ""  // Empty - loaded from Key Vault
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",  // Reduce verbosity
      "Microsoft.AspNetCore": "Error"
    }
  },
  "Features": {
    "RealtimePipeline": {
      "Enabled": true  // Enable when implemented
    }
  }
}
```text

**Secrets injected at runtime (Azure managed identity):**

```bash
export ASPNETCORE_ENVIRONMENT=Production
export KEYVAULT_ENDPOINT=https://gtek-fsm.vault.azure.net
# Application automatically loads secrets on startup
dotnet GTEK.FSM.Backend.Api.dll
````

---

## Configuration Validation

### Check Current Configuration

````csharp
// In Startup/Program.cs:
var config = builder.Configuration;

// Log effective configuration (debug only)
foreach (var kvp in config.AsEnumerable())
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}
```text

### Validate at Build Time

```bash
# Build and check for configuration issues
dotnet build -c Release

# View warnings/errors in output
````

### Test Configuration Locally

````bash
# Create test .env
cp .env.example .env.test

# Edit with test values
nano .env.test

# Load and run
export $(cat .env.test | xargs)
dotnet run
```text

---

## Configuration by Phase

### Phase 0 (Current)

- ✅ Database connection (local)
- ✅ API ports and hosts
- ✅ Logging levels
- ✅ Feature flags (all disabled)
- ⏳ Code quality rules (appsettings, EditorConfig)

### Phase 1

- 🔜 Domain models in database config
- 🔜 Migration history in database

### Phase 2 (Identity)

- 🔜 JWT secret management (Key Vault)
- 🔜 OAuth client credentials
- 🔜 Tenant isolation configuration

### Phase 3+ (Services)

- 🔜 Enable external services (Notifications, Maps, Payments)
- 🔜 Storage provider selection (Local → Blob/S3)
- 🔜 Webhook endpoints and secrets

### Phase 11 (Production)

- 🔜 Production Key Vault integration
- 🔜 Managed identity authentication
- 🔜 Monitoring and alerting config
- 🔜 High-availability database config

---

## Troubleshooting Configuration

### Issue: Configuration not loading

**Debug:**

```bash
# Add to Program.cs temporary
var config = builder.Configuration;
Console.WriteLine(config["Database:ConnectionString"]);

# Check .env file exists
ls -la .env

# Check environment variable is set
echo $ASPNETCORE_ENVIRONMENT
````

### Issue: Connection string has special characters

**Solution:** URL-encode special characters or use full connection builder:

````csharp
var builder = new SqlConnectionStringBuilder
{
    DataSource = config["Database:Server"],
    InitialCatalog = config["Database:Database"],
    UserID = config["Database:User"],
    Password = config["Database:Password"],  // Special chars OK here
    Encrypt = true
};
var connectionString = builder.ConnectionString;
```text

### Issue: Different developers have different config

**Solution:** Commit `appsettings.json` and `appsettings.Development.json`, let `.env` vary per machine:

```bash
# .gitignore
.env
.env.local
.env.*.local
````

---

## References

- [.env.example](.env.example) — Complete variable list
- [appsettings.json](backend/api/appsettings.json) — Base configuration
- [Kubernetes secrets](https://kubernetes.io/docs/concepts/configuration/secret/) — Future multi-tenant config
- [Azure Key Vault](https://learn.microsoft.com/azure/key-vault/)
