# GTEK FSM - Local Development Setup Guide

## Quick Start (5 minutes)

### Prerequisites

- **OS**: Ubuntu 22.04+ (Linux), macOS 12+, or Windows 11 with WSL2
- **.NET SDK 10.0+**: [Download](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Docker & Docker Compose**: [Install](https://docs.docker.com/get-docker/)
- **Git**: [Install](https://git-scm.com/downloads)
- **VS Code** (recommended): [Download](https://code.visualstudio.com/)

Verify installations:
```bash
dotnet --version        # Should be 10.0+
docker --version        # Should be 20.10+
docker-compose --version  # Should be 2.0+
git --version           # Any recent version
```

### Clone & Setup (First Run Only)

```bash
# 1. Clone repository
git clone https://github.com/shamil-suraweera/gtek-fsm.git
cd gtek-fsm

# 2. Copy environment file
cp .env.example .env

# 3. Edit .env with local values (optional for default local dev)
# Default values work for Docker-based local development
code .env  # or nano .env

# 4. First-time solution setup
dotnet restore GTEK.FSM.slnx

# 5. Done! Skip to "Daily Development" section below
```

---

## Daily Development Workflow

### Option 1: Full Stack (Recommended)

Start everything with one command:

```bash
./deploy/scripts/start-all.sh
```

This runs:
- SQL Server in Docker
- Backend API (localhost:5000)
- Displays instructions for Web Portal and Mobile App

**In separate terminals:**
```bash
# Terminal 2: Web Portal (auto-reload)
./deploy/scripts/run-web-portal.sh

# Terminal 3: Mobile App (build, requires Android SDK on Linux)
./deploy/scripts/run-mobile-app.sh

# Terminal 4: Watch logs
./deploy/scripts/dev-logs.sh
```

**Access:**
- Backend API: http://localhost:5000
- API Health: http://localhost:5000/health
- Web Portal: http://localhost:5001
- DB: localhost:1433 (via SQL client)

---

### Option 2: API + Database Only

```bash
./deploy/scripts/run-api-standalone.sh
```

This automatically:
1. Starts SQL Server container
2. Applies migrations
3. Launches API on localhost:5000

---

### Option 3: Web Portal Only

```bash
./deploy/scripts/run-web-portal.sh
```

- Blazor WebAssembly dev server
- Hot-reload on file changes
- Available at localhost:5001

---

## VS Code Setup

### Recommended Extensions

1. **C# Dev Kit** (`ms-dotnettools.csharp`)
   - Code completion, debugging, test runner
   
2. **EditorConfig** (`editorconfig.editorconfig`)
   - Auto-applies formatting rules

3. **Docker** (`ms-vscode.docker`)
   - Container management UI

4. **Thunder Client** or **Rest Client**
   - API testing (optional)

### Quick Start in VS Code

```
Ctrl+K Ctrl+O → Select gtek-fsm folder
Ctrl+Shift+B → Build Solution
Ctrl+Shift+D → Launch Debug Configuration (API)
```

---

## Configuration Reference

### Environment Variables (`.env`)

All configuration options defined in [.env.example](.env.example):

- **Database**: SQL Server host, port, credentials
- **API**: Host, port, scheme
- **Feature Flags**: All disabled in Phase 0
- **Services**: Placeholders for SignalR, Storage, Notifications, etc.

### Application Settings (appsettings.json)

Three-tier hierarchy (most specific wins):

```
appsettings.json (base)
    ↓
appsettings.{ASPNETCORE_ENVIRONMENT}.json (environment-specific)
    ↓
Environment Variables (highest priority)
```

**Example:**
```bash
# In .env:
ASPNETCORE_ENVIRONMENT=Development

# loads: appsettings.json + appsettings.Development.json
# Environment variables override both
```

---

## Common Tasks

### Database Management

```bash
# Apply pending migrations to local database
make dev-db-init

# Reset database to fresh state
make dev-db-reset

# Insert seed data (placeholder in Phase 0)
make dev-db-seed
```

### Building

```bash
# Build entire solution
make build

# Build just the API
make build-api

# Build with release optimization
dotnet build GTEK.FSM.slnx -c Release
```

### Cleaning

```bash
# Remove build artifacts (bin/obj)
make clean

# Reset Docker containers and volumes
make dev-reset

# Full cleanup + rebuild
make clean && make build
```

### Debugging

**Visual Studio Code:**
1. Set breakpoint (click line number)
2. Press F5 or Ctrl+Shift+D
3. Select ".NET Core: API (Launch)"
4. Breakpoint hit: variables visible in sidebar

**Command Line:**
```bash
# Run with debug symbols
dotnet build -c Debug
dotnet run --project backend/api/GTEK.FSM.Backend.Api.csproj
```

---

## Troubleshooting

### Issue: Docker not running / Permission denied

```bash
# Solution 1: Start Docker daemon
docker daemon

# Solution 2: Add user to docker group (Linux)
sudo usermod -aG docker $USER
newgrp docker

# Verify
docker run hello-world
```

### Issue: Port already in use (5000, 5001, 1433)

```bash
# Check what's using port 5000
lsof -i :5000

# Kill process
kill -9 <PID>

# Or use different port in .env
API_PORT=5555
```

### Issue: SQL Server container won't start

```bash
# Check container logs
docker logs sqlserver

# Common: Invalid SA_PASSWORD (< 8 chars, missing special chars)
# Fix: Update SA_PASSWORD in .env to strong password
# Restart: make dev-reset
```

### Issue: Solution won't build

```bash
# Clear cache and rebuild
rm -rf ~/.nuget/packages/*gtek* 2>/dev/null
dotnet clean GTEK.FSM.slnx
dotnet restore GTEK.FSM.slnx
dotnet build GTEK.FSM.slnx
```

### Issue: Web Portal shows blank screen

```bash
# 1. Browser cache: Ctrl+Shift+Delete → Clear browsing data
# 2. Rebuild portal project
make build-portal

# 3. Stop and restart
Ctrl+C
./deploy/scripts/run-web-portal.sh
```

### Issue: Analyzer warnings block build (Phase 0.8.2)

```bash
# Check CODE_QUALITY_BASELINE.md for warnings
# Fix code or add justified suppression (see ANALYZER_SUPPRESSIONS_GUIDE.md)

# Build with details
dotnet build --verbosity detailed
```

---

## Environment Setup Examples

### Local Development (.env)

```bash
ASPNETCORE_ENVIRONMENT=Development
SA_PASSWORD=YourStrong!Passw0rd
SQL_SERVER_HOST=sqlserver  # Docker service name
API_PORT=5000
FEATURE_REALTIME_PIPELINE=false  # Disabled in Phase 0
```

### Team CI/CD (GitHub Actions)

Configured in `.github/workflows/ci.yml`:
- Restores packages
- Builds (Debug + Release)
- Runs analyzers
- Discovers tests

No secrets needed for public repo; Phase 2 will add Key Vault integration.

### Production (Future Phase 11)

Will use:
- Azure Key Vault for secrets
- Managed Identity for auth
- Connection strings from Key Vault
- Health checks & load balancing

Example (not active yet):
```bash
ASPNETCORE_ENVIRONMENT=Production
# All secrets from Key Vault
# Connection strings from managed identity
```

---

## First-Time Team Onboarding

1. **Clone & Setup** (5 min)
   ```bash
   git clone <repo>
   cd gtek-fsm
   cp .env.example .env
   ```

2. **Install Extensions** (2 min)
   - Open VS Code → Extensions → Install C# Dev Kit, EditorConfig

3. **Verify Build** (2 min)
   ```bash
   dotnet restore
   dotnet build
   ```

4. **Start Services** (1 min)
   ```bash
   ./deploy/scripts/start-all.sh
   ```

5. **Test Access** (1 min)
   ```bash
   curl http://localhost:5000/health
   ```

**Total: ~10 minutes to full development environment**

---

## Staying Current

### Daily Sync

```bash
git pull origin dev
dotnet restore
dotnet build
```

### When Code Quality Changes

The `.editorconfig` and `.stylecop.json` are automatically applied by VS Code. If violations appear on build:

```bash
# See config/CODE_QUALITY_BASELINE.md for guidance
# Fix violations or add justified suppressions
```

### When Migrations Are Added (Phase 1+)

```bash
git pull
make dev-db-reset
make dev-db-init
```

---

## Next Steps

### Recommended Reading

1. **Build Scripts:** [deploy/BUILD_SCRIPTS_GUIDE.md](../deploy/BUILD_SCRIPTS_GUIDE.md)
2. **Code Quality:** [config/CODE_QUALITY_BASELINE.md](../config/CODE_QUALITY_BASELINE.md)
3. **CI/CD:** [config/CI_PIPELINE_GUIDE.md](../config/CI_PIPELINE_GUIDE.md)
4. **Architecture:** [config/ARCHITECTURE_RULES.md](../config/architecture-rules.json)

### Phase 1 Preview

Domain models and database schema will be defined. You'll:

```bash
# Generate new migrations
dotnet ef migrations add NewFeature \
  --project backend/infrastructure \
  --startup-project backend/api \
  --verbose

# Apply to database
make dev-db-init
```

---

## Getting Help

**Questions?**
1. Check troubleshooting section above
2. Review referenced documentation files
3. Check `backend/api/Program.cs` for DI setup
4. VS Code: F1 → "Developer: Show Extension Output" for error details

**Reporting Issues:**
- Include error message + stack trace
- Mention your OS and .NET version
- Attach your .env (with passwords redacted)

