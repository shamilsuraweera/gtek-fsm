#!/bin/bash
# Run the API server locally. SQL Server must already be running on this machine.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

if [[ ! -f ".env" ]]; then
    echo "❌ .env file not found."
    exit 1
fi

set -a
source .env
set +a

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export Database__ConnectionString="Server=${SQL_SERVER_HOST:-localhost},${SQL_SERVER_PORT:-1433};Database=${SQL_DATABASE:-GTEK_FSM_Local};User Id=sa;Password=${SA_PASSWORD};Encrypt=true;TrustServerCertificate=true;"

echo "=========================================="
echo "Starting API (Local)"
echo "=========================================="
echo ""
echo "   Environment: $ASPNETCORE_ENVIRONMENT"
echo "   Database:    ${SQL_DATABASE:-GTEK_FSM_Local} on ${SQL_SERVER_HOST:-localhost}:${SQL_SERVER_PORT:-1433}"
echo ""

echo "📦 Applying pending migrations..."
./database/scripts/dev-db-init.sh

echo ""
echo "🚀 Starting API server..."
echo "   API:    http://localhost:${API_PORT:-5000}"
echo "   Health: http://localhost:${API_PORT:-5000}/health"
echo ""

cd backend/api
dotnet run --configuration Debug

