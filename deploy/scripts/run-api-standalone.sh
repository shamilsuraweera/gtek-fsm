#!/bin/bash
# Run the API server with SQL Server in Docker
# Ensures database is initialized before starting API
# Prerequisites: Docker must be running, SQL Server image available

set -e

PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$PROJECT_ROOT"

echo "=========================================="
echo "Starting API (Standalone - Docker SQL)"
echo "=========================================="
echo ""

# Check if Docker is running
if ! docker info > /dev/null 2>&1; then
    echo "❌ Docker is not running. Please start Docker and try again."
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

# Ensure SQL Server container is running
if ! docker ps --filter "name=sqlserver" --quiet | grep -q .; then
    echo "🐳 Starting SQL Server container..."
    $COMPOSE_CMD up -d sqlserver
    echo "⏳ Waiting for SQL Server to be healthy..."
    sleep 15
fi

# Apply any pending migrations
echo "📦 Applying database migrations..."
./database/scripts/dev-db-init.sh || true

# Start the API server
echo ""
echo "🚀 Starting API server..."
echo "   API will be available at: http://localhost:5000"
echo "   Health check: http://localhost:5000/health"
echo ""

cd backend/api
dotnet run --configuration Debug

