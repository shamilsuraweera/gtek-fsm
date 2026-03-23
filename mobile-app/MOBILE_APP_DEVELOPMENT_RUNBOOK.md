# Mobile App Development Runbook

**Phase 6.5.4 Artifact** - Setup, common issues, and troubleshooting paths for local mobile development.

## Scope

This runbook covers development for the MAUI mobile app at `mobile-app/customer-worker`.

Audience:

- Mobile developers
- Full-stack developers running end-to-end local flows

Out of scope:

- Production mobile release operations
- App store signing/distribution

## Quick Start

- Ensure prerequisites are installed.
- Validate local environment with preflight checks.
- Build (and optionally run) the app.
- Use troubleshooting paths if a step fails.

Core script:

```bash
./deploy/scripts/run-mobile-app.sh
```

## Setup

### Prerequisites

Required:

- .NET 10 SDK
- MAUI workload
- Android SDK (Linux) or Xcode tooling (macOS)
- Java SDK for Android tooling
- Android emulator or physical device (for run mode)

Install MAUI workload:

```bash
dotnet workload restore
dotnet workload install maui
```

### Environment Variables

Linux Android defaults:

```bash
export ANDROID_SDK_ROOT=/path/to/Android/Sdk
export ANDROID_HOME=$ANDROID_SDK_ROOT
```

Optional Java override:

```bash
export JavaSdkDirectory=/path/to/jdk
```

Optional mobile/runtime values:

- `MOBILE_ENSURE_BACKEND=1` to start `sqlserver` + `backend-api`
- `MOBILE_RECOVER=1` to auto-recover from common startup/build failures (default enabled)
- `MOBILE_CLEAR_CACHE=1` to force clean caches before build
- `MOBILE_START_EMULATOR_IF_DOWN=1` to auto-start emulator in run mode
- `MOBILE_EMULATOR_NAME=<avd-name>` emulator profile used for auto-start
- `GTEK_FSM_API_BASE_URL` override API endpoint for emulator/device routing
- `GTEK_FSM_API_PORT` API port for conflict checks and adb reverse (default `5000`)

### Recommended Daily Commands

Preflight only (idempotent validation):

```bash
./deploy/scripts/run-mobile-app.sh --preflight-only
```

Build Android app:

```bash
./deploy/scripts/run-mobile-app.sh
```

Build and run on Android target:

```bash
./deploy/scripts/run-mobile-app.sh --run
```

Build with explicit stale-cache cleanup first:

```bash
./deploy/scripts/run-mobile-app.sh --clear-cache
```

Auto-start emulator if no device is connected:

```bash
MOBILE_EMULATOR_NAME=Pixel_7_API_35 ./deploy/scripts/run-mobile-app.sh --run --start-emulator-if-down
```

## Verification Checklist

After setup, confirm all checks:

- `./deploy/scripts/run-mobile-app.sh --preflight-only` returns success.
- `./deploy/scripts/run-mobile-app.sh` completes Android build successfully.
- If running app locally, one device appears via `adb devices`.
- If backend is required, API health is reachable at `http://localhost:5000/health` (or configured port).

## Common Issues

### 1. Android SDK not found

Symptoms:

- Script exits with message to set `ANDROID_SDK_ROOT` or `ANDROID_HOME`.

Fix:

- Export SDK path in shell.
- Re-run preflight check.

### 2. Emulator/device unavailable

Symptoms:

- Run mode fails with: emulator/device is not available.

Fix:

- Start emulator manually from Android Studio, or
- Use auto-start mode with `MOBILE_EMULATOR_NAME` and `--start-emulator-if-down`.

### 3. `adb` command not found

Symptoms:

- Script reports adb CLI missing.

Fix:

- Add Android platform-tools to `PATH`.
- Confirm with `adb version`.

### 4. API port conflict on 5000

Symptoms:

- Script reports port is already in use.

Fix:

- Keep recovery mode enabled (`MOBILE_RECOVER=1`) to continue with existing API, or
- Stop conflicting process, or
- Use `GTEK_FSM_API_BASE_URL` to explicitly target desired API endpoint.

### 5. Build fails due to stale caches/intermediate state

Symptoms:

- Build fails unexpectedly after SDK/workload/tooling updates.

Fix:

- Run with `--clear-cache`, or
- Keep `MOBILE_RECOVER=1` so script performs one clean+retry cycle.

### 6. Java SDK validation warning on Linux

Symptoms:

- Warning from Xamarin.Android tooling around Java SDK path validation.

Fix:

- Set `JavaSdkDirectory` to a readable JDK path.
- Retry build.

### 7. Authenticated API calls fail in app

Symptoms:

- App screens fall back to placeholders or show auth errors.

Fix:

- Ensure identity token is available (`GTEK_FSM_IDENTITY_TOKEN`).
- Ensure token is not expired.
- Verify API endpoint variables and backend availability.

## Troubleshooting Paths

Use this path-based flow to resolve issues quickly.

### Path A: Script fails before build starts

- Run preflight only:

```bash
./deploy/scripts/run-mobile-app.sh --preflight-only
```

- If failure mentions SDK/JDK, fix environment variables.
- If failure mentions Docker Compose while `MOBILE_ENSURE_BACKEND=1`, either install Compose or start backend manually.
- Re-run preflight until success.

### Path B: Build fails during Android compile

- Retry with forced cache cleanup:

```bash
./deploy/scripts/run-mobile-app.sh --clear-cache
```

- If still failing, run direct build for fuller output:

```bash
dotnet build mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj -f net10.0-android -c Debug
```

- Resolve workload/toolchain issues (`dotnet workload repair` if needed), then retry.

### Path C: Run mode fails with no device/emulator

- Check devices:

```bash
adb devices
```

- If none, start emulator manually or run auto-start mode:

```bash
MOBILE_EMULATOR_NAME=<avd-name> ./deploy/scripts/run-mobile-app.sh --run --start-emulator-if-down
```

- Retry run command after emulator boot completes.

### Path D: App cannot reach backend API

- Confirm API health:

```bash
curl -f http://localhost:${GTEK_FSM_API_PORT:-5000}/health
```

- If using emulator, prefer host mapping with `10.0.2.2` via `GTEK_FSM_API_BASE_URL`.
- If script reports port conflict, decide whether to keep existing API or stop conflicting process.
- Retry app run.

### Path E: Auth or tenant context issues in mobile flows

- Verify token source and expiry (`GTEK_FSM_IDENTITY_TOKEN`).
- Re-run app and review mobile diagnostics state/log output.
- Cross-check backend auth and tenant guidance in `backend/SECURITY_DEVELOPER_RUNBOOK.md`.

## Logs and Diagnostics

Useful commands:

```bash
# Build output with Android target
dotnet build mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj -f net10.0-android -c Debug

# Connected Android targets
adb devices

# API health
curl -f http://localhost:${GTEK_FSM_API_PORT:-5000}/health
```

Related scripts:

- `deploy/scripts/run-mobile-app.sh`
- `deploy/scripts/start-all.sh`
- `deploy/scripts/dev-logs.sh`

## Escalation

If the above paths do not resolve the issue, capture and share:

- Exact command used.
- Full console output.
- OS and SDK versions (`dotnet --info`, Android SDK, Java SDK).
- Whether issue reproduces with `--preflight-only`, `--clear-cache`, and direct `dotnet build`.
