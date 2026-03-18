#!/bin/bash
# Run the Web Portal with hot-reload (watch mode)
# Displays the portal at http://localhost:5001

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

echo "=========================================="
echo "Starting Web Portal (Watch Mode)"
echo "=========================================="
echo ""
echo "🔍 Watch mode enabled - changes will auto-rebuild"
echo "   Portal will be available at: http://localhost:5001"
echo "   Ctrl+C to stop"
echo ""

cd web-portal
dotnet watch run --configuration Debug --project GTEK.FSM.WebPortal.csproj

