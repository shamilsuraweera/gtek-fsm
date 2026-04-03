#!/bin/bash
# Build and run the Mobile App.
# On Linux, supports Android builds (requires Android SDK).
# For iOS/macOS/Windows targets, run on respective platforms.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

RUN_APP=0
PREFLIGHT_ONLY=0
TARGET_DEVICE=""
START_EMULATOR_IF_DOWN="${MOBILE_START_EMULATOR_IF_DOWN:-0}"
MOBILE_EMULATOR_NAME="${MOBILE_EMULATOR_NAME:-}"
RECOVER_ON_FAILURE="${MOBILE_RECOVER:-1}"
CLEAR_CACHE_FIRST="${MOBILE_CLEAR_CACHE:-0}"
API_PORT="${GTEK_FSM_API_PORT:-${API_PORT:-5000}}"

usage() {
    cat <<EOF
Usage: ./deploy/scripts/run-mobile-app.sh [options]

Options:
  --run                         Build and run on emulator/device after build.
  --device <id>                 Device id for run mode.
  --start-emulator-if-down      Auto-start emulator if none is running (requires MOBILE_EMULATOR_NAME).
  --preflight-only              Run validations and recovery checks only.
  --clear-cache                 Clean stale build caches before build.
  --no-recover                  Disable automatic stale-cache recovery/retry.
  --api-port <port>             API port used for conflict checks and adb reverse (default: 5000).
  --help                        Show this help.

Environment:
  MOBILE_ENSURE_BACKEND=1       Start sqlserver + backend-api via Docker Compose.
  MOBILE_EMULATOR_NAME=<avd>    AVD name used with --start-emulator-if-down.
  MOBILE_RECOVER=1              Auto-recover from stale cache build failures.
  MOBILE_CLEAR_CACHE=1          Clean caches before build.
EOF
}

while [[ $# -gt 0 ]]; do
    case "$1" in
        --run)
            RUN_APP=1
            shift
            ;;
        --device)
            TARGET_DEVICE="${2:-}"
            if [[ -z "$TARGET_DEVICE" ]]; then
                echo "❌ --device requires a value."
                exit 1
            fi
            shift 2
            ;;
        --start-emulator-if-down)
            START_EMULATOR_IF_DOWN=1
            shift
            ;;
        --preflight-only)
            PREFLIGHT_ONLY=1
            shift
            ;;
        --clear-cache)
            CLEAR_CACHE_FIRST=1
            shift
            ;;
        --no-recover)
            RECOVER_ON_FAILURE=0
            shift
            ;;
        --api-port)
            API_PORT="${2:-}"
            if [[ -z "$API_PORT" ]]; then
                echo "❌ --api-port requires a value."
                exit 1
            fi
            shift 2
            ;;
        --help)
            usage
            exit 0
            ;;
        *)
            echo "❌ Unknown option: $1"
            usage
            exit 1
            ;;
    esac
done

detect_compose_cmd() {
    if docker compose version > /dev/null 2>&1; then
        echo "docker compose"
        return
    fi

    if command -v docker-compose > /dev/null 2>&1; then
        echo "docker-compose"
        return
    fi

    echo ""
}

has_listening_port() {
    local port="$1"
    ss -ltn "sport = :$port" | awk 'NR>1 { print }' | grep -q .
}

active_android_devices() {
    if ! command -v adb >/dev/null 2>&1; then
        return 0
    fi

    adb devices 2>/dev/null | awk 'NR>1 && $2 == "device" {print $1}'
}

wait_for_emulator_boot() {
    local timeout_seconds=120
    local started_at
    started_at=$(date +%s)

    adb wait-for-device >/dev/null 2>&1 || true

    while true; do
        if [[ "$(adb shell getprop sys.boot_completed 2>/dev/null | tr -d '\r')" == "1" ]]; then
            return 0
        fi

        if (( $(date +%s) - started_at >= timeout_seconds )); then
            return 1
        fi

        sleep 2
    done
}

start_emulator_if_needed() {
    if [[ -n "$(active_android_devices)" ]]; then
        return 0
    fi

    if [[ "$START_EMULATOR_IF_DOWN" != "1" ]]; then
        return 1
    fi

    if ! command -v adb >/dev/null 2>&1; then
        echo "❌ Android adb CLI not found in PATH."
        echo "   Add <AndroidSdk>/platform-tools to PATH and retry."
        return 1
    fi

    if [[ -z "$MOBILE_EMULATOR_NAME" ]]; then
        echo "❌ No emulator is running and MOBILE_EMULATOR_NAME is not set."
        echo "   Start an emulator manually, or set MOBILE_EMULATOR_NAME and use --start-emulator-if-down."
        return 1
    fi

    if ! command -v emulator >/dev/null 2>&1; then
        echo "❌ Android emulator CLI not found in PATH."
        echo "   Add <AndroidSdk>/emulator to PATH and retry."
        return 1
    fi

    echo "📱 Starting emulator '$MOBILE_EMULATOR_NAME'..."
    nohup emulator -avd "$MOBILE_EMULATOR_NAME" > /tmp/gtek-mobile-emulator.log 2>&1 &

    if ! wait_for_emulator_boot; then
        echo "❌ Emulator did not become ready in time."
        echo "   Check /tmp/gtek-mobile-emulator.log for boot issues."
        return 1
    fi

    echo "✅ Emulator is online."
    return 0
}

clean_stale_cache() {
    echo "🧹 Cleaning stale mobile build cache..."
    dotnet clean -f net10.0-android -c Debug || true
    rm -rf bin/Debug/net10.0-android obj/Debug/net10.0-android
    dotnet nuget locals http-cache --clear >/dev/null 2>&1 || true
}

configure_adb_reverse() {
    local device="$1"
    local port="$2"

    if [[ -z "$device" ]]; then
        return 0
    fi

    adb -s "$device" reverse --remove "tcp:$port" >/dev/null 2>&1 || true

    if adb -s "$device" reverse "tcp:$port" "tcp:$port" >/dev/null 2>&1; then
        echo "🔁 Configured adb reverse for tcp:$port on $device"
    else
        echo "⚠️  Could not configure adb reverse for tcp:$port on $device"
    fi
}

build_android() {
    echo "📦 Restoring dependencies..."
    dotnet restore

    if [[ "$CLEAR_CACHE_FIRST" == "1" ]]; then
        clean_stale_cache
    fi

    echo "🔨 Building for Android..."
    if dotnet build -f net10.0-android -c Debug; then
        return 0
    fi

    if [[ "$RECOVER_ON_FAILURE" != "1" ]]; then
        return 1
    fi

    echo "⚠️  Android build failed. Attempting stale-cache recovery and retry once..."
    clean_stale_cache
    dotnet restore
    dotnet build -f net10.0-android -c Debug
}

start_backend_if_requested() {
    local compose_cmd="$1"

    if [[ "${MOBILE_ENSURE_BACKEND:-0}" != "1" ]]; then
        return 0
    fi

    if has_listening_port "$API_PORT"; then
        if [[ "$RECOVER_ON_FAILURE" == "1" ]]; then
            echo "⚠️  Port $API_PORT is already in use."
            echo "   Recovery mode enabled: skipping backend startup and using existing API endpoint."
            echo "   Set GTEK_FSM_API_BASE_URL (for example http://10.0.2.2:$API_PORT) if needed."
            return 0
        fi

        echo "❌ Port $API_PORT is already in use and may conflict with backend startup."
        echo "   Stop the conflicting process or run with MOBILE_RECOVER=1 to auto-recover."
        return 1
    fi

    if [[ -z "$compose_cmd" ]]; then
        echo "⚠️  Docker Compose not found. Skipping backend dependency startup."
        echo "   Install Docker Compose or run backend services manually."
        return 0
    fi

    echo "🐳 Starting backend dependencies (sqlserver + backend-api)..."
    $compose_cmd up -d sqlserver backend-api
    echo ""
}

run_maui_target() {
    local target_framework="$1"
    local target_device="${2:-}"
    local run_log=""

    run_log="$(mktemp -t gtek-mobile-run.XXXXXX.log)"

    if dotnet maui --help >/dev/null 2>&1; then
        if [[ -n "$target_device" ]]; then
            if dotnet maui run -f "$target_framework" -c Debug --device "$target_device" -p:EmbedAssembliesIntoApk=true 2>&1 | tee "$run_log"; then
                rm -f "$run_log"
                return 0
            fi
        else
            if dotnet maui run -f "$target_framework" -c Debug -p:EmbedAssembliesIntoApk=true 2>&1 | tee "$run_log"; then
                rm -f "$run_log"
                return 0
            fi
        fi
    else
        # Newer SDK installations may not expose the `dotnet maui` verb.
        # `dotnet build -t:Run` is the compatible CLI fallback.
        if [[ -n "$target_device" ]]; then
            export ANDROID_SERIAL="$target_device"
        fi

        if dotnet build -t:Run -f "$target_framework" -c Debug -p:EmbedAssembliesIntoApk=true 2>&1 | tee "$run_log"; then
            rm -f "$run_log"
            return 0
        fi
    fi

    if grep -q "INSTALL_FAILED_USER_RESTRICTED" "$run_log"; then
        echo ""
        echo "❌ Android installation was blocked by device policy/user confirmation (INSTALL_FAILED_USER_RESTRICTED)."
        echo "   Checklist:"
        echo "   1) Keep device unlocked and screen on during install."
        echo "   2) Accept any on-device install/security confirmation prompts."
        echo "   3) In Developer Options, enable USB debugging and install-via-USB (if available)."
        echo "   4) If policy prompts persist, uninstall existing app then retry:"
        echo "      adb uninstall com.companyname.gtek.fsm.mobileapp"
        echo ""
    fi

    rm -f "$run_log"
    return 1
}

recover_stale_device_install() {
    local target_device="$1"

    if [[ -z "$target_device" ]]; then
        return 1
    fi

    echo "🛠️  Recovery: removing stale app install from '$target_device'..."
    adb -s "$target_device" uninstall com.companyname.gtek.fsm.mobileapp >/dev/null 2>&1 || true
    return 0
}

echo "=========================================="
echo "Building Mobile App"
echo "=========================================="
echo ""

COMPOSE_CMD="$(detect_compose_cmd)"
start_backend_if_requested "$COMPOSE_CMD"

if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "🐧 Linux detected - building for Android"
    echo ""

    if [[ -z "${ANDROID_SDK_ROOT:-}" && -z "${ANDROID_HOME:-}" ]]; then
        echo "❌ Android SDK not found. Set ANDROID_SDK_ROOT or ANDROID_HOME."
        echo "   MAUI requires Android SDK 21+ for builds."
        exit 1
    fi

    cd mobile-app/customer-worker

    if [[ "$PREFLIGHT_ONLY" == "1" ]]; then
        if [[ "$RUN_APP" == "1" ]]; then
            if ! start_emulator_if_needed; then
                echo "❌ Preflight failed: emulator is not available."
                exit 1
            fi
        fi

        echo "✅ Preflight checks passed."
        exit 0
    fi

    build_android

    if [[ "$RUN_APP" == "1" ]]; then
        if ! start_emulator_if_needed; then
            echo "❌ Cannot run app because no Android emulator/device is available."
            exit 1
        fi

        if [[ -z "$TARGET_DEVICE" ]]; then
            TARGET_DEVICE="$(active_android_devices | head -n 1)"
        fi

        configure_adb_reverse "$TARGET_DEVICE" "$API_PORT"

        if [[ -n "$TARGET_DEVICE" ]]; then
            echo "🚀 Running app on device/emulator '$TARGET_DEVICE'..."
            if ! run_maui_target net10.0-android "$TARGET_DEVICE"; then
                if [[ "$RECOVER_ON_FAILURE" == "1" ]]; then
                    recover_stale_device_install "$TARGET_DEVICE"
                    echo "🔁 Retrying deployment after recovery..."
                    run_maui_target net10.0-android "$TARGET_DEVICE"
                else
                    exit 1
                fi
            fi
        else
            echo "🚀 Running app on default Android target..."
            run_maui_target net10.0-android
        fi
    else
        echo ""
        echo "✅ Mobile app built successfully!"
        echo "   To run on emulator: ./deploy/scripts/run-mobile-app.sh --run"
        echo "   To run on device: ./deploy/scripts/run-mobile-app.sh --run --device <device-id>"
        echo "   CLI fallback if dotnet maui is unavailable: dotnet build -t:Run -f net10.0-android -c Debug mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj"
        echo "   Optional backend bootstrap: MOBILE_ENSURE_BACKEND=1 ./deploy/scripts/run-mobile-app.sh"
        echo "   Recovery mode: MOBILE_RECOVER=1 ./deploy/scripts/run-mobile-app.sh --run"
    fi
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "🍎 macOS detected - can build for iOS, macOS, and Android"
    echo ""

    cd mobile-app/customer-worker

    if [[ "$PREFLIGHT_ONLY" == "1" ]]; then
        echo "✅ Preflight checks passed."
        exit 0
    fi

    echo "📦 Restoring dependencies..."
    dotnet restore

    echo "🔨 Building for iOS..."
    dotnet build -f net10.0-ios -c Debug

    echo ""
    echo "✅ Mobile app built for iOS!"
    echo "   To run on simulator: dotnet build -t:Run -f net10.0-ios -c Debug"
    echo "   Optional backend bootstrap: MOBILE_ENSURE_BACKEND=1 ./deploy/scripts/run-mobile-app.sh"
else
    echo "❌ Unsupported platform: $OSTYPE"
    echo "   Mobile app builds are supported on Linux (Android) and macOS (iOS/Android)"
    exit 1
fi

