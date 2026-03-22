#!/bin/bash
# Run the Web Portal with hot-reload (watch mode)
# Displays the portal at http://localhost:5001

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

WEB_PORT="${WEB_PORTAL_PORT:-5001}"

echo "=========================================="
echo "Starting Web Portal (Watch Mode)"
echo "=========================================="
echo ""
echo "🔍 Watch mode enabled - changes will auto-rebuild"
echo "   Portal will be available at: http://localhost:${WEB_PORT}"
echo "   Ctrl+C to stop"
echo ""

# Linux environments often hit inotify limits with dotnet watch.
# Polling mode avoids hard crashes when watch limits are low.
export DOTNET_USE_POLLING_FILE_WATCHER=1
export ASPNETCORE_URLS="http://localhost:${WEB_PORT}"
export _SkipUpgradeNetAnalyzersNuGetWarning=true

cd web-portal
dotnet watch run --no-launch-profile --configuration Debug --project GTEK.FSM.WebPortal.csproj

