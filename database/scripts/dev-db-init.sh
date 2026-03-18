#!/usr/bin/env bash
set -euo pipefail

# Initializes local development database by applying all EF Core migrations.
# Usage:
#   ./database/scripts/dev-db-init.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
INFRA_PROJECT="$ROOT_DIR/backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj"
API_PROJECT="$ROOT_DIR/backend/api/GTEK.FSM.Backend.Api.csproj"
DB_CONTEXT="GtekFsmDbContext"

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"

echo "[dev-db-init] Environment: $ASPNETCORE_ENVIRONMENT"
echo "[dev-db-init] Applying migrations..."

dotnet ef database update \
  --project "$INFRA_PROJECT" \
  --startup-project "$API_PROJECT" \
  --context "$DB_CONTEXT"

echo "[dev-db-init] Database initialized and up to date."
