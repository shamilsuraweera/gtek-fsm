#!/bin/bash
# Build and run the Mobile App
# On Linux, supports Android builds (requires Android SDK)
# For iOS/macOS/Windows targets, run on respective platforms

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

# Detect Docker Compose command (v2: `docker compose`, v1: `docker-compose`).
if docker compose version > /dev/null 2>&1; then
    COMPOSE_CMD="docker compose"
elif command -v docker-compose > /dev/null 2>&1; then
    COMPOSE_CMD="docker-compose"
else
    COMPOSE_CMD=""
fi

echo "=========================================="
echo "Building Mobile App"
echo "=========================================="
echo ""

# Optional helper: start backend dependencies for end-to-end mobile testing.
if [[ "${MOBILE_ENSURE_BACKEND:-0}" == "1" ]]; then
    if [[ -n "$COMPOSE_CMD" ]]; then
        echo "🐳 Starting backend dependencies (sqlserver + backend-api)..."
        $COMPOSE_CMD up -d sqlserver backend-api
        echo ""
    else
        echo "⚠️  Docker Compose not found. Skipping backend dependency startup."
        echo "   Install Docker Compose or run backend services manually."
        echo ""
    fi
fi

# Detect platform
if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    echo "🐧 Linux detected - building for Android"
    echo ""
    
    # Check if Android SDK is available
    if [ -z "$ANDROID_SDK_ROOT" ] && [ -z "$ANDROID_HOME" ]; then
        echo "⚠️  Android SDK not found. Set ANDROID_SDK_ROOT or ANDROID_HOME environment variable."
        echo "   MAUI requires Android SDK 21+ for builds."
        echo ""
        echo "   For Ubuntu, install via:"
        echo "   dotnet workload restore"
        echo "   dotnet workload install maui"
        exit 1
    fi
    
    cd mobile-app/customer-worker
    
    echo "📦 Restoring dependencies..."
    dotnet restore
    
    echo "🔨 Building for Android..."
    dotnet build -f net10.0-android -c Debug
    
    echo ""
    echo "✅ Mobile app built successfully!"
    echo "   To run on emulator: dotnet maui run -f net10.0-android -c Debug"
    echo "   To run on device: dotnet maui run -f net10.0-android -c Debug --device <device-id>"
    echo "   Optional backend bootstrap: MOBILE_ENSURE_BACKEND=1 ./deploy/scripts/run-mobile-app.sh"
    
elif [[ "$OSTYPE" == "darwin"* ]]; then
    echo "🍎 macOS detected - can build for iOS, macOS, and Android"
    echo ""
    
    cd mobile-app/customer-worker
    
    echo "📦 Restoring dependencies..."
    dotnet restore
    
    echo "🔨 Building for iOS..."
    dotnet build -f net10.0-ios -c Debug
    
    echo ""
    echo "✅ Mobile app built for iOS!"
    echo "   To run on simulator: dotnet maui run -f net10.0-ios -c Debug"
    echo "   Optional backend bootstrap: MOBILE_ENSURE_BACKEND=1 ./deploy/scripts/run-mobile-app.sh"
    
else
    echo "❌ Unsupported platform: $OSTYPE"
    echo "   Mobile app builds are supported on Linux (Android) and macOS (iOS/Android)"
    exit 1
fi

