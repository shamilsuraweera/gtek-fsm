#!/usr/bin/env bash
set -euo pipefail

# Resets local development database and reapplies migrations.
# Usage:
#   ./database/scripts/dev-db-reset.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INFRA_PROJECT="$ROOT_DIR/backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj"
API_PROJECT="$ROOT_DIR/backend/api/GTEK.FSM.Backend.Api.csproj"
DB_CONTEXT="GtekFsmDbContext"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

echo "[dev-db-reset] Environment: $ASPNETCORE_ENVIRONMENT"
echo "[dev-db-reset] Dropping local database..."

dotnet ef database drop \
  --project "$INFRA_PROJECT" \
  --startup-project "$API_PROJECT" \
  --context "$DB_CONTEXT" \
  --force

echo "[dev-db-reset] Reapplying migrations..."

dotnet ef database update \
  --project "$INFRA_PROJECT" \
  --startup-project "$API_PROJECT" \
  --context "$DB_CONTEXT"

echo "[dev-db-reset] Database reset complete."
