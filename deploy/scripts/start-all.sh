#!/bin/bash
# Start all services in orchestrated order:
# 1. SQL Server + API (Docker Compose)
# 2. Web Portal (separate terminal recommended)
# 3. Mobile App (build only on Linux)

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

if [[ ! -f ".env" ]]; then
    echo "❌ .env file not found. Create it from .env.example before starting the stack."
    echo "   Example: cp .env.example .env"
    exit 1
fi

# Export .env values so compose and child scripts use the same configuration.
set -a
source .env
set +a

# Provide defaults when .env omits optional compose keys.
export API_PORT="${API_PORT:-5000}"
export SQL_DATABASE="${SQL_DATABASE:-GTEK_FSM_Local}"
export SQL_SERVER_PORT="${SQL_SERVER_PORT:-1433}"

# Default dockerized API startup to Development (can be overridden if needed).
STACK_ASPNETCORE_ENVIRONMENT="${START_STACK_ENVIRONMENT:-Development}"
export ASPNETCORE_ENVIRONMENT="$STACK_ASPNETCORE_ENVIRONMENT"

echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║         GTEK FSM - Full Development Stack              ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""

# Check Docker
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker first."
    exit 1
fi

# Resolve Docker Compose command for broad compatibility.
if docker compose version > /dev/null 2>&1; then
    COMPOSE_CMD="docker compose"
elif command -v docker-compose > /dev/null 2>&1; then
    COMPOSE_CMD="docker-compose"
else
    echo "❌ Docker Compose is not available. Install Docker Compose v2 or docker-compose."
    exit 1
fi

# 1. Start infrastructure (SQL + API)
echo "📦 Phase 1: Starting infrastructure (SQL Server + API)..."
echo "   Command: $COMPOSE_CMD up -d"
echo ""

# Ensure startup is idempotent and avoids stale container name conflicts.
$COMPOSE_CMD down --remove-orphans > /dev/null 2>&1 || true
$COMPOSE_CMD up -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 10

# 2. Initialize database
echo ""
echo "📊 Phase 2: Initializing database..."
if [[ -n "${SA_PASSWORD:-}" ]]; then
    DB_CONN="Server=localhost,${SQL_SERVER_PORT};Database=${SQL_DATABASE};User Id=sa;Password=${SA_PASSWORD};Encrypt=true;TrustServerCertificate=true;"
    ASPNETCORE_ENVIRONMENT=Development Database__ConnectionString="$DB_CONN" ./database/scripts/dev-db-init.sh || true
else
    echo "⚠️  SA_PASSWORD is not set; skipping DB migration initialization."
fi

# 3. Build and display instructions
echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║              🚀 Services Starting Up 🚀               ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "✅ SQL Server:"
echo "   Host:     localhost (in Docker: sqlserver)"
echo "   Port:     ${SQL_SERVER_PORT}"
echo "   Database: ${SQL_DATABASE}"
echo "   Auth:     SQL Authentication"
echo ""
echo "✅ Backend API:"
echo "   URL:      http://localhost:${API_PORT}"
echo "   Health:   http://localhost:${API_PORT}/health"
echo "   Status:   Running in Docker"
echo ""
echo "📋 Next Steps (Run in Separate Terminals):"
echo ""
echo "   Terminal 1 - Web Portal (Hot-reload):"
echo "   $ ./deploy/scripts/run-web-portal.sh"
echo "   → http://localhost:5001"
echo ""
echo "   Terminal 2 - Mobile App (Build only on Linux):"
echo "   $ ./deploy/scripts/run-mobile-app.sh"
echo ""
echo "   Terminal 3 - View Service Logs:"
echo "   $ ./deploy/scripts/dev-logs.sh"
echo ""
echo "   To Stop All Services:"
echo "   $ ./deploy/scripts/dev-down.sh"
echo ""
echo "───────────────────────────────────────────────────────────"
echo ""
echo "✨ Infrastructure is now running!"
echo "   To stop containers:"
echo "   $ ./deploy/scripts/dev-down.sh"
echo ""

