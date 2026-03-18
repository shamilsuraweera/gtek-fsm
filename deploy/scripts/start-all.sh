#!/bin/bash
# Start all services in orchestrated order:
# 1. SQL Server + API (Docker Compose)
# 2. Web Portal (separate terminal recommended)
# 3. Mobile App (build only on Linux)

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

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

# 1. Start infrastructure (SQL + API)
echo "📦 Phase 1: Starting infrastructure (SQL Server + API)..."
echo "   Command: docker-compose up -d"
echo ""
docker-compose up -d

echo ""
echo "⏳ Waiting for services to be ready..."
sleep 10

# 2. Initialize database
echo ""
echo "📊 Phase 2: Initializing database..."
./database/scripts/dev-db-init.sh || true

# 3. Build and display instructions
echo ""
echo "╔════════════════════════════════════════════════════════╗"
echo "║              🚀 Services Starting Up 🚀               ║"
echo "╚════════════════════════════════════════════════════════╝"
echo ""
echo "✅ SQL Server:"
echo "   Host:     localhost (in Docker: sqlserver)"
echo "   Port:     1433"
echo "   Database: GTEK_FSM_Local"
echo "   Auth:     SQL Authentication"
echo ""
echo "✅ Backend API:"
echo "   URL:      http://localhost:5000"
echo "   Health:   http://localhost:5000/health"
echo "   Status:   Starting in background..."
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

# Start API in background
cd backend/api
echo "Starting API server (logs available via: make dev-logs)..."
dotnet run --configuration Debug > /tmp/api.log 2>&1 &
API_PID=$!
echo "API PID: $API_PID"

echo ""
echo "✨ Full stack is now running!"
echo "   Press Ctrl+C in any terminal to stop, then run:"
echo "   $ make dev-down"
echo ""

# Keep script alive
wait

