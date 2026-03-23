# GTEK FSM

GTEK FSM is a multi-tenant Facility Service Management platform with:

- Mobile app for Customers and Workers
- Web portal for Management and Customer Care
- Shared .NET and MS SQL backend

## Current Status

This repository is in early scaffolding stage (Phase 0).

## Repository Structure

- `backend/` - API and backend services
- `web-portal/` - management and customer care web app
- `mobile-app/` - customer and worker mobile app
- `shared/` - shared contracts, models, and design assets
- `database/` - database assets and migration strategy
- `deploy/` - deployment and runtime orchestration assets
- `config/` - environment and configuration templates

## Project Boundaries

- `backend/domain/` - domain entities, value objects, and domain rules
- `backend/application/` - use cases and application orchestration
- `backend/infrastructure/` - persistence and external integrations
- `backend/api/` - HTTP host and transport layer
- `shared/contracts/` - shared API and cross-client contracts
- `web-portal/customer-care/` and `web-portal/management/` - web client areas
- `mobile-app/customer-worker/` - shared mobile client area for customer and worker flows

## Naming Conventions

Naming conventions are defined in `config/naming-conventions.json`.

## Tenancy and Architecture Rules

- Tenancy approach is defined in `config/tenancy-approach.json`.
- Baseline layering and dependency rules are defined in `config/architecture-rules.json`.

## Roadmap

See `roadmap.txt` for the phase-by-phase plan.

## Local Auth Token Validation (Phase 2)

- Copy `backend/api/.env.auth.example` to `backend/api/.env.auth.local` and set local values.
- Start API with matching JWT env values (or local appsettings overrides).
- Generate a token for local/dev checks:
  - `./backend/api/scripts/dev-auth-token.sh`
- Run bootstrap auth probe checks (`401`, `403`, `200` paths):
  - `./backend/api/scripts/dev-auth-bootstrap-check.sh`

Notes:

- `backend/api/.env.auth.local` is gitignored.
- Do not commit real signing keys to repository-tracked files.

## Mobile Development Prerequisites (Phase 6)

For Linux/Ubuntu mobile development, use Android target builds.

Required:

- .NET 10 SDK
- MAUI workload: `dotnet workload install maui`
- Android SDK (API 21+) and Java SDK
- Android emulator (or physical Android device)

Environment setup (Linux):

- Set Android SDK path in your shell profile:
  - `export ANDROID_SDK_ROOT=/path/to/Android/Sdk`
  - `export ANDROID_HOME=$ANDROID_SDK_ROOT`
- Optional Java SDK override if auto-detection is restricted:
  - `export JavaSdkDirectory=/path/to/jdk`

Optional `.env` entries for mobile:

- `MOBILE_ENSURE_BACKEND=1` to auto-start `sqlserver` and `backend-api` when running mobile script
- `GTEK_FSM_API_BASE_URL` to fully override API base URL used by mobile app
- `GTEK_FSM_API_PORT` to override debug local API port (default `5000`)

Run mobile build/start script:

- `./deploy/scripts/run-mobile-app.sh`
- Preflight only (idempotent environment checks): `./deploy/scripts/run-mobile-app.sh --preflight-only`
- Build + run on Android with recovery enabled: `./deploy/scripts/run-mobile-app.sh --run`
- Force stale-cache cleanup before build: `./deploy/scripts/run-mobile-app.sh --clear-cache`

Then run app on emulator/device from project root:

- Emulator: `dotnet maui run -f net10.0-android -c Debug`
- Device: `dotnet maui run -f net10.0-android -c Debug --device <device-id>`

Recovery-focused notes:

- Emulator down: use `--start-emulator-if-down` and set `MOBILE_EMULATOR_NAME=<avd-name>`.
- API port conflict: with `MOBILE_ENSURE_BACKEND=1`, recovery mode (`MOBILE_RECOVER=1`, default) skips backend startup if port is busy and keeps using the existing API endpoint.
- Stale build cache: use `--clear-cache` or rely on automatic single retry recovery when a build fails.

Detailed mobile setup and troubleshooting runbook:

- `mobile-app/MOBILE_APP_DEVELOPMENT_RUNBOOK.md`

Phase completion and handoff document:

- `mobile-app/PHASE6_COMPLETION_HANDOFF.md`
