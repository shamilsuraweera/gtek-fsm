# GTEK FSM

GTEK FSM is a multi-tenant Facility Service Management system built on .NET.
It includes:

- Backend API (ASP.NET Core)
- Web Portal (Blazor)
- Mobile App (MAUI)
- SQL Server database

## What GTEK FSM Is

GTEK FSM (Facility Service Management) is an end-to-end service operations platform for handling facility issues from intake to closure across customer, worker, and management channels.

The platform is composed of:

- A .NET backend API with MS SQL persistence
- A web portal for management and customer-care operations
- A mobile app for customers and field workers
- A real-time operational pipeline (Kanban-style) for service-request lifecycle visibility

Core operational capabilities and product goals include:

- Service request lifecycle orchestration (New, Assigned, InProgress, OnHold, Completed, Cancelled)
- Worker assignment workflows, with roadmap support for road-distance, skills, and internal rating based matching
- SLA-driven execution model (response, assignment, and completion timers)
- Real-time updates across channels for status and assignment changes
- Push-notification integration roadmap for mobile users
- Full audit trail for sensitive and operational actions
- Multi-tenant security boundaries and role-based access control
- Minimalistic UI direction with support for light and dark themes across clients

The system is designed to keep operations tenant-safe, observable, and consistent across backend, web, and mobile channels while supporting phased delivery.

## AI Execution Note

If you are an AI coding/analysis agent operating in this repository, read this file (`README.md`) first before making changes. Use it as the primary operational context for project purpose, setup, run flows, and test commands.

This guide is an operational manual for local setup, run, and testing.

## Project Layout

- `backend/` - API, domain, application, and infrastructure layers
- `web-portal/` - management and customer-care web UI
- `mobile-app/` - customer/worker MAUI app and tests
- `database/` - migration and seed scripts
- `deploy/` - local orchestration scripts
- `shared/` - shared contracts
- `config/` - architecture, tenancy, and conventions docs

## Prerequisites

Install the following:

- .NET SDK 10+
- Docker + Docker Compose
- SQL Server client tools (`sqlcmd`) for seed/verify scripts
- Git

For mobile development on Linux:

- MAUI workload: `dotnet workload install maui`
- Android SDK + adb + emulator
- Java SDK

## One-Time Setup

### 1. Clone and enter the repository

```bash
git clone https://github.com/shamil-suraweera/gtek-fsm.git
cd gtek-fsm
```

### 2. Create local environment file

```bash
cp .env.example .env
```

### 3. Set at least these values in `.env`

- `ASPNETCORE_ENVIRONMENT=Development`
- `SA_PASSWORD=YourStrong!Passw0rd`
- `SQL_SERVER_PORT=1433`
- `SQL_DATABASE=GTEK_FSM_Local`
- `API_PORT=5000`
- `WEB_PORTAL_PORT=5001`

### 4. Restore dependencies

```bash
dotnet restore GTEK.FSM.slnx
```

## Run The System

### Option A: Full Stack Helper

Starts SQL Server + API in Docker and prints the next commands for portal/mobile.

```bash
./deploy/scripts/start-all.sh
```

Then in separate terminals:

```bash
./deploy/scripts/run-web-portal.sh
./deploy/scripts/run-mobile-app.sh
./deploy/scripts/dev-logs.sh
```

### Option B: API + SQL Only

Starts SQL container (if needed), applies migrations, then runs API locally.

```bash
./deploy/scripts/run-api-standalone.sh
```

### Option C: Docker Compose Infrastructure

Start API + SQL in detached mode:

```bash
./deploy/scripts/dev-up.sh
```

Stop containers:

```bash
./deploy/scripts/dev-down.sh
```

Reset containers and volumes:

```bash
./deploy/scripts/dev-reset.sh
```

Follow logs:

```bash
./deploy/scripts/dev-logs.sh
```

## Database Workflow

Apply migrations:

```bash
./database/scripts/dev-db-init.sh
```

Drop and recreate database:

```bash
./database/scripts/dev-db-reset.sh
```

Apply SQL seed files from `database/seeds/`:

```bash
./database/scripts/dev-db-seed.sh
```

Verify required schema tables and baseline data counts:

```bash
./database/scripts/dev-db-verify.sh
```

Do a full refresh (reset + init + seed + verify):

```bash
./database/scripts/dev-db-refresh.sh
```

Notes:

- `dev-db-seed.sh` and `dev-db-verify.sh` require `sqlcmd` and a valid `SA_PASSWORD`/`SQL_PASSWORD`.
- Verification checks tables such as `Tenants`, `Users`, `ServiceRequests`, `Jobs`, and `Subscriptions`.

## Web Portal

Run with watch mode and hot reload:

```bash
./deploy/scripts/run-web-portal.sh
```

Default URL: `http://localhost:5001`

## Mobile App

Build mobile app:

```bash
./deploy/scripts/run-mobile-app.sh
```

Preflight only:

```bash
./deploy/scripts/run-mobile-app.sh --preflight-only
```

Build and run (Android):

```bash
./deploy/scripts/run-mobile-app.sh --run
```

If your SDK does not expose the `dotnet maui` command, use:

```bash
dotnet build -t:Run -f net10.0-android -c Debug mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj
```

Useful flags:

- `--device <id>`
- `--start-emulator-if-down`
- `--clear-cache`
- `--api-port <port>`

Useful environment variables:

- `MOBILE_ENSURE_BACKEND=1` (auto-start backend dependencies via compose)
- `MOBILE_EMULATOR_NAME=<avd-name>`
- `MOBILE_RECOVER=1` (auto-recover stale cache)
- `GTEK_FSM_API_BASE_URL=<url>`

## Build Commands

Build everything:

```bash
dotnet build GTEK.FSM.slnx -c Debug
```

Build API only:

```bash
dotnet build backend/api/GTEK.FSM.Backend.Api.csproj -c Debug
```

Build Web Portal:

```bash
dotnet build web-portal/GTEK.FSM.WebPortal.csproj -c Debug
```

Build Mobile Android target:

```bash
dotnet build mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj -f net10.0-android -c Debug
```

Equivalent Make targets are available:

```bash
make build
make build-api
make build-portal
make build-mobile
```

## Test Commands

Run all tests in solution:

```bash
dotnet test GTEK.FSM.slnx -c Debug
```

Run test projects individually:

```bash
dotnet test backend/domain.tests/GTEK.FSM.Backend.Domain.Tests.csproj -c Debug
dotnet test backend/infrastructure.tests/GTEK.FSM.Backend.Infrastructure.Tests.csproj -c Debug
dotnet test web-portal.tests/GTEK.FSM.WebPortal.Tests.csproj -c Debug
dotnet test mobile-app/customer-worker.tests/GTEK.FSM.MobileApp.Tests.csproj -c Debug
```

Auth bootstrap probe checks:

```bash
./backend/api/scripts/dev-auth-bootstrap-check.sh
```

Generate local token for auth checks:

```bash
./backend/api/scripts/dev-auth-token.sh
```

## Service Endpoints

- API base: `http://localhost:5000`
- API health: `http://localhost:5000/health`
- Web portal: `http://localhost:5001`
- SQL Server: `localhost:1433`

## VS Code Task Labels

If you use the VS Code Task Runner, these are mapped already:

- `Build: Solution (restore + build)`
- `Build: API Only`
- `Build: Web Portal`
- `Build: Mobile App (Android)`
- `Run: API (Standalone - SQL Server in Docker)`
- `Run: API + SQL Server (Docker Compose)`
- `Run: Web Portal (Watch + Dev Server)`
- `Run: Mobile App (Android - Debug)`
- `Run: All (API + Portal + Mobile)`
- `Stop: All Services`
- `Database: Init (Apply Migrations)`
- `Database: Reset (Drop + Recreate)`
- `Database: Seed`
- `Database: Refresh (Reset + Init + Seed + Verify)`
- `Database: Verify Schema & Data`

## Troubleshooting

Docker not running:

```bash
docker info
```

If this fails, start Docker and retry.

Port conflict check:

```bash
ss -ltnp | grep -E ':5000|:5001|:1433'
```

Clean build outputs:

```bash
make clean
```

Linux debugger attach permission error (`vsdbg has insufficient privileges`):

```bash
echo 0 | sudo tee /proc/sys/kernel/yama/ptrace_scope
```

To persist across reboot, configure `kernel.yama.ptrace_scope=0` in `/etc/sysctl.d/`.

MAUI run command not found (`dotnet-maui does not exist`):

```bash
dotnet build -t:Run -f net10.0-android -c Debug mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj
```

Android install blocked (`INSTALL_FAILED_USER_RESTRICTED`):

```bash
adb uninstall com.companyname.gtek.fsm.mobileapp
./deploy/scripts/run-mobile-app.sh --run
```

Also ensure the device/emulator is unlocked, confirm any on-device install prompt, and enable USB debugging/install-via-USB in Developer Options.

## Additional Guides

- `LOCAL_SETUP_GUIDE.md`
- `DOCKER_SETUP_GUIDE.md`
- `mobile-app/MOBILE_APP_DEVELOPMENT_RUNBOOK.md`
