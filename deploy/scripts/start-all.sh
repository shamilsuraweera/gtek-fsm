#!/bin/bash
# Start all services locally. SQL Server must already be running on this machine.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

if [[ ! -f ".env" ]]; then
    echo "❌ .env file not found. Create it from .env.example before starting the stack."
    echo "   Example: cp .env.example .env"
    exit 1
fi

set -a
source .env
set +a

export API_PORT="${API_PORT:-5000}"
export SQL_DATABASE="${SQL_DATABASE:-GTEK_FSM_Local}"
export SQL_SERVER_PORT="${SQL_SERVER_PORT:-1433}"
export SQL_SERVER_HOST="${SQL_SERVER_HOST:-localhost}"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

DB_CONN="Server=${SQL_SERVER_HOST},${SQL_SERVER_PORT};Database=${SQL_DATABASE};User Id=sa;Password=${SA_PASSWORD};Encrypt=true;TrustServerCertificate=true;"

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║         GTEK FSM - Full Development Stack              ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# Verify SQL Server is reachable before trying migrations
echo "🔍 Checking SQL Server connectivity..."
if command -v sqlcmd &>/dev/null; then
    if ! sqlcmd -S "${SQL_SERVER_HOST},${SQL_SERVER_PORT}" \
                -U sa -P "${SA_PASSWORD}" -C -Q "SELECT 1" > /dev/null 2>&1; then
        echo "❌ Cannot reach SQL Server at ${SQL_SERVER_HOST}:${SQL_SERVER_PORT}."
        echo "   Make sure SQL Server is running and SA_PASSWORD in .env is correct."
        exit 1
    fi
    echo "✅ SQL Server is reachable."
else
    echo "⚠️  sqlcmd not found — skipping connectivity pre-check."
fi

echo ""
echo "📊 Applying database migrations..."
Database__ConnectionString="$DB_CONN" ./database/scripts/dev-db-init.sh

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║              🚀 Ready to Start Services 🚀             ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "✅ SQL Server:  ${SQL_SERVER_HOST}:${SQL_SERVER_PORT}  (${SQL_DATABASE})"
echo "✅ Migrations:  applied"
echo ""
echo "📋 Run each service in a separate terminal:"
echo ""
echo "   Terminal 1 - Backend API:"
echo "   $ ./deploy/scripts/run-api-standalone.sh"
echo "   → http://localhost:${API_PORT}"
echo ""
echo "   Terminal 2 - Web Portal (hot-reload):"
echo "   $ ./deploy/scripts/run-web-portal.sh"
echo "   → http://localhost:${WEB_PORTAL_PORT:-5001}"
echo ""
echo "   Terminal 3 - Mobile App (Android):"
echo "   $ ./deploy/scripts/run-mobile-app.sh --run"
echo ""
echo "───────────────────────────────────────────────────────────"

