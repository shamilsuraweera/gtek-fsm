# Build Scripts and VS Code Tasks Guide

This document describes the build automation and VS Code tasks for the GTEK FSM platform on Ubuntu and other environments.

## Quick Start

### Option 1: VS Code Tasks (Recommended for Development)

Press `Ctrl+Shift+B` (or `Cmd+Shift+B` on macOS) to open the VS Code task picker, then select:

- **Build: Solution (restore + build)** — Full solution rebuild
- **Run: API (Standalone - SQL Server in Docker)** — Start just the API with SQL container
- **Run: Web Portal (Watch + Dev Server)** — Web portal with hot-reload
- **Run: All (API + Portal + Mobile)** — Full orchestrated startup

### Option 2: Shell Scripts (Direct Terminal Use)

````bash
# Build entire solution
dotnet build GTEK.FSM.slnx

# Start API + SQL Server
./deploy/scripts/run-api-standalone.sh

# Start Web Portal (watch mode)
./deploy/scripts/run-web-portal.sh

# Build Mobile App (Android on Linux)
./deploy/scripts/run-mobile-app.sh

# Start everything orchestrated
./deploy/scripts/start-all.sh

# Stop all services
./deploy/scripts/dev-down.sh
```text

### Option 3: Makefile Targets

```bash
make build          # Build entire solution
make dev-up         # Start API + SQL Server containers
make dev-down       # Stop all containers
make dev-logs       # Tail service logs
make dev-db-init    # Apply database migrations
make dev-db-reset   # Drop and recreate database
````

---

## VS Code Tasks

### Build Tasks

#### Build: Solution (restore + build)

- **Default build task** (Ctrl+Shift+B)
- Restores NuGet packages and builds entire solution
- Uses Debug configuration
- Runs all project builds in dependency order

#### Build: API Only

- Builds only `backend/api/GTEK.FSM.Backend.Api.csproj`
- Faster for focused API development
- Skips web portal and mobile app builds

#### Build: Web Portal

- Builds only `web-portal/GTEK.FSM.WebPortal.csproj`
- For Blazor WebAssembly portal changes

#### Build: Mobile App (Android)

- Builds `mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj` for Android
- Ubuntu-only; Windows/macOS support separate targets
- Requires Android SDK (via `dotnet workload install maui`)

### Run Tasks

#### Run: API (Standalone - SQL Server in Docker)

- Starts Docker SQL Server container if needed
- Applies pending migrations automatically
- Launches API server on `http://localhost:5000`
- Health check available at `http://localhost:5000/health`

#### Run: API + SQL Server (Docker Compose)

- Equivalent to `make dev-up`
- Starts both sql and api services via docker-compose
- Maintains container state across runs

#### Run: Web Portal (Watch + Dev Server)

- Starts Blazor WebAssembly dev server with hot-reload
- Available at `http://localhost:5001`
- Watches `.razor`, `.cs`, and `.css` files for changes
- Ctrl+C to stop

#### Run: Mobile App (Android - Debug)

- Detects platform (Linux = Android, macOS = iOS + Android)
- Shows instructions for running on emulator or device
- Requires Android SDK setup on Linux

#### Run: All (API + Portal + Mobile)

- Orchestrated startup of full development stack
- Starts SQL + API in background
- Prints instructions for portal and mobile in separate terminals
- Provides single cleanup command

#### Stop: All Services

- Runs `docker-compose down`
- Stops and removes running containers
- Preserves named volume `gtek-fsm-sql-data` (database persists)
- Use `make dev-reset` to remove volume and reset database

### Database Tasks

#### Database: Init (Apply Migrations)

- Applies pending EF Core migrations to local database
- Equivalent to `dotnet ef database update`
- Runs if already in Docker compose; creates container if needed

#### Database: Reset (Drop + Recreate)

- Drops entire database
- Reapplies migrations from ground zero
- Useful for testing seed data or resolving schema conflicts
- Does **not** reset containers; use `make dev-reset` for that

#### Database: Seed

- Runs seed script pipeline (placeholder in Phase 0.7)
- Discovers and executes SQL seed files from `database/seeds/`
- Intended for reference data population in future phases

### Utility Tasks

#### Clean: Build Artifacts

- Removes all `bin/` and `obj/` folders
- Useful for resolving build cache issues
- Runs silently, doesn't display output

---

## Shell Scripts

All scripts are executable (`chmod +x`) and located in `deploy/scripts/`:

### run-api-standalone.sh

```text
Usage: ./deploy/scripts/run-api-standalone.sh

Purpose:
  - Ensures SQL Server container is running (starts if needed)
  - Applies pending database migrations
  - Launches API server on http://localhost:5000

Requirements:
  - Docker must be running
  - No other API instances on port 5000

Output:
  - Real-time API logs to console
  - Press Ctrl+C to stop
```

### run-web-portal.sh

```text
Usage: ./deploy/scripts/run-web-portal.sh

Purpose:
  - Starts Blazor WebAssembly dev server with hot-reload
  - Watches for changes and rebuilds automatically

Available at:
  - http://localhost:5001

Output:
  - Hot-reload notifications to console
  - Press Ctrl+C to stop
```

### run-mobile-app.sh

```text
Usage: ./deploy/scripts/run-mobile-app.sh

Purpose:
  - Detects platform (Linux vs macOS)
  - Builds Mobile App for appropriate target

On Linux (Android):
  - Restores packages
  - Builds for net10.0-android
  - Shows emulator/device run instructions

On macOS:
  - Builds for iOS
  - Shows iOS simulator run instructions

Requirements:
  - Android SDK (Linux): Set ANDROID_SDK_ROOT or run 'dotnet workload install maui'
  - Xcode (macOS): For iOS builds

Output:
  - Build progress and final instructions
```

### start-all.sh

```text
Usage: ./deploy/scripts/start-all.sh

Purpose:
  - Orchestrated startup of entire development stack
  - Phase 1: SQL Server + API (Docker Compose background)
  - Phase 2: Database initialization
  - Phase 3: Displays instructions for Web Portal and Mobile in separate terminals

Output:
  - Service status display
  - Terminal-by-terminal instructions
  - Commands to stop all services

Note:
  - Web Portal and Mobile App should be started in separate terminals
  - API runs in background; see logs with './deploy/scripts/dev-logs.sh'
```

---

## Makefile Targets

All targets are convenience wrappers around shell scripts or dotnet commands:

````makefile
make build              # dotnet build GTEK.FSM.slnx -c Debug
make dev-db-init       # ./database/scripts/dev-db-init.sh
make dev-db-reset      # ./database/scripts/dev-db-reset.sh
make dev-db-seed       # ./database/scripts/dev-db-seed.sh
make dev-up            # ./deploy/scripts/dev-up.sh
make dev-down          # ./deploy/scripts/dev-down.sh
make dev-logs          # ./deploy/scripts/dev-logs.sh
make dev-reset         # ./deploy/scripts/dev-reset.sh
```text

---

## Common Workflows

### Fresh Start (Full Stack)

```bash
# 1. Build everything
make build

# OR via VS Code (Ctrl+Shift+B)
# Select: Build: Solution (restore + build)

# 2. Start services
./deploy/scripts/start-all.sh

# 3. In separate terminals:
./deploy/scripts/run-web-portal.sh      # Terminal 1
./deploy/scripts/run-mobile-app.sh      # Terminal 2
./deploy/scripts/dev-logs.sh            # Terminal 3 (watch logs)

# 4. Access services:
# API:     http://localhost:5000
# Portal:  http://localhost:5001
# Health:  http://localhost:5000/health
````

### API-Only Development

````bash
# 1. Build just API
make build-api            # OR Ctrl+Shift+B → "Build: API Only"

# 2. Start API + SQL
./deploy/scripts/run-api-standalone.sh
# OR via VS Code task: Ctrl+Shift+B → "Run: API (Standalone)"

# 3. Test in another terminal
curl http://localhost:5000/health
```text

### Web Portal Development

```bash
# 1. Start in watch mode
./deploy/scripts/run-web-portal.sh
# Portal available at http://localhost:5001
# Changes auto-rebuild and hot-reload

# 2. Access API (requires separate API terminal)
# API should already be running from API-only workflow
````

### Database Management

````bash
# Reset database to latest migration state
make dev-db-reset

# Apply pending migrations
make dev-db-init

# Seed reference data (Schema defined, execution pending Phase 1)
make dev-db-seed

# View container logs
make dev-logs
```text

### Cleanup and Recovery

```bash
# Stop all services
make dev-down

# Full reset: Stop containers and remove volumes
make dev-reset

# Clean build artifacts and rebuild
make clean
make build

# Restart everything
./deploy/scripts/start-all.sh
````

---

## Environment Configuration

### Local Development (.env files)

Create `.env` file in project root (already provided `.env.example`):

````bash
# Copy template
cp .env.example .env

# Edit with your settings
COMPOSE_PROJECT_NAME=gtek-fsm-dev
SQL_SA_PASSWORD=YourStrongPassword123!
ASPNETCORE_ENVIRONMENT=Development
```text

### Connection Strings

- **Docker SQL**: Server = sqlserver,1433; User ID = sa; Password = $SA_PASSWORD
- **Local SQL**: Server = localhost; Authentication = Windows (or SQL auth)
- **API Config**: `backend/api/appsettings.Development.json`

---

## Troubleshooting

### Docker not running

````

Error: Docker is not running
Fix: Start Docker daemon or Docker Desktop

```text

### Port already in use

```

Error: Address already in use (port 5000, 5001, 1433)
Fix:

- Stop conflicting service: lsof -i :5000
- Or use different port in launchSettings.json

```text

### Android SDK not found

```

Error: ANDROID_SDK_ROOT not set
Fix:

- Linux: dotnet workload install maui
- Or set: export ANDROID_SDK_ROOT=/path/to/android/sdk

```text

### Database migrations not applying

```

Error: "No executed migrations"
Fix:

- Verify SQL Server is running: docker ps
- Check connection string in appsettings
- Run: ./database/scripts/dev-db-init.sh

```text

### Hot-reload not working (Web Portal)

```

Fix:

- Stop current session (Ctrl+C)
- Run: make clean
- Restart: ./deploy/scripts/run-web-portal.sh

```text

---

## Performance Notes

- **Docker Compose overhead**: First start ~30s, subsequent starts ~3s
- **Full solution build**: ~10s (debug), ~20s (release)
- **API startup**: ~2s (with existing DB)
- **Web Portal hot-reload**: ~1-3s per change
- **Mobile build (Android)**: ~60s first build, ~20s incremental

---

## Next Steps (Phase 0.8.2+)

- Add code formatting (`dotnet format`)
- Add linting rules (StyleCop, Roslyn analyzers)
- Add test project discovery and running
- Add pre-commit hooks for quality checks
```
