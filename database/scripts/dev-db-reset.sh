#!/usr/bin/env bash
set -euo pipefail

# Resets local development database and reapplies migrations.
# Usage:
#   ./database/scripts/dev-db-reset.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INFRA_PROJECT="$ROOT_DIR/backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj"
DB_CONTEXT="GtekFsmDbContext"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

# Load .env for Docker SQL Server credentials if present
ENV_FILE="$ROOT_DIR/.env"
if [[ -f "$ENV_FILE" ]]; then
    set -o allexport
    # shellcheck source=/dev/null
    source "$ENV_FILE"
    set +o allexport
fi

# Build a local-accessible SQL Server connection string from .env values.
# This overrides the appsettings.Development.json default (Integrated Security / Server=.)
# which cannot connect to the Docker container.
SQL_HOST="${SQL_SERVER_HOST:-localhost}"
SQL_PORT="${SQL_SERVER_PORT:-12433}"
SQL_DB="${SQL_DATABASE:-GTEK_FSM_Local}"
SQL_PASS="${SA_PASSWORD:-}"

if [[ -n "$SQL_PASS" ]]; then
    export Database__ConnectionString="Server=${SQL_HOST},${SQL_PORT};Database=${SQL_DB};User Id=sa;Password=${SQL_PASS};Encrypt=true;TrustServerCertificate=true;"
fi

echo "[dev-db-reset] Environment: $ASPNETCORE_ENVIRONMENT"
echo "[dev-db-reset] Dropping local database..."

dotnet ef database drop \
  --project "$INFRA_PROJECT" \
  --startup-project "$INFRA_PROJECT" \
  --context "$DB_CONTEXT" \
  --force

echo "[dev-db-reset] Reapplying migrations..."

dotnet ef database update \
  --project "$INFRA_PROJECT" \
  --startup-project "$INFRA_PROJECT" \
  --context "$DB_CONTEXT"

echo "[dev-db-reset] Database reset complete."
