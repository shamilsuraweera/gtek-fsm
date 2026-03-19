#!/usr/bin/env bash
set -euo pipefail

# Verification script for schema and seed data.
# Checks that all Phase 1 tables exist and seed data is properly applied.
# Usage:
#   ./database/scripts/dev-db-verify.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SEEDS_DIR="$ROOT_DIR/database/seeds"
ENV_FILE="$ROOT_DIR/.env"

if [[ -f "$ENV_FILE" ]]; then
  # shellcheck disable=SC1090
  source "$ENV_FILE"
fi

SQL_HOST="${SQL_SERVER_HOST:-localhost}"
SQL_PORT="${SQL_SERVER_PORT:-${SQL_SERVER_TCP_PORT:-1433}}"
SQL_DATABASE="${SQL_SERVER_DATABASE:-${SQL_DATABASE:-GTEK_FSM_Local}}"
SQL_USER="${SQL_USER:-sa}"
SQL_PASSWORD="${SA_PASSWORD:-${SQL_PASSWORD:-}}"

if ! command -v sqlcmd >/dev/null 2>&1; then
  echo "[dev-db-verify] sqlcmd not found. Skipping verification."
  exit 0
fi

if [[ -z "$SQL_PASSWORD" ]]; then
  echo "[dev-db-verify] SA_PASSWORD/SQL_PASSWORD is not set. Skipping verification."
  exit 0
fi

echo "[dev-db-verify] Target: ${SQL_HOST},${SQL_PORT} / ${SQL_DATABASE}"

# Check for required tables
REQUIRED_TABLES=("Tenants" "Users" "ServiceRequests" "Jobs" "Subscriptions" "__EFMigrationsHistory")

echo "[dev-db-verify] Checking for required schema tables..."
for table in "${REQUIRED_TABLES[@]}"; do
  table_exists=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
    "SET NOCOUNT ON; SELECT COUNT(1) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'$table';" | tr -d '[:space:]')
  
  if [[ "$table_exists" == "1" ]]; then
    echo "  ✓ $table"
  else
    echo "  ✗ MISSING: $table"
    exit 1
  fi
done
echo ""

# Check baseline seed data
echo "[dev-db-verify] Checking baseline seed data..."

# Expected seed data counts (from 001_baseline_reference_data.sql)
EXPECTED_TENANTS=1
EXPECTED_USERS=6
EXPECTED_SUBSCRIPTIONS=3
EXPECTED_REQUESTS=6
EXPECTED_JOBS=6

# Query actual counts
ACTUAL_TENANTS=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
  "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.Tenants WHERE IsDeleted = 0;" | tr -d '[:space:]')

ACTUAL_USERS=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
  "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.Users WHERE IsDeleted = 0;" | tr -d '[:space:]')

ACTUAL_SUBSCRIPTIONS=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
  "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.Subscriptions WHERE IsDeleted = 0;" | tr -d '[:space:]')

ACTUAL_REQUESTS=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
  "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.ServiceRequests WHERE IsDeleted = 0;" | tr -d '[:space:]')

ACTUAL_JOBS=$(/opt/mssql-tools/bin/sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q \
  "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.Jobs WHERE IsDeleted = 0;" | tr -d '[:space:]')

# Validate counts
VERIFICATION_PASSED=true

if [[ "$ACTUAL_TENANTS" == "$EXPECTED_TENANTS" ]]; then
  echo "  ✓ Tenants: $ACTUAL_TENANTS (expected $EXPECTED_TENANTS)"
else
  echo "  ✗ Tenants: $ACTUAL_TENANTS (expected $EXPECTED_TENANTS)"
  VERIFICATION_PASSED=false
fi

if [[ "$ACTUAL_USERS" == "$EXPECTED_USERS" ]]; then
  echo "  ✓ Users: $ACTUAL_USERS (expected $EXPECTED_USERS)"
else
  echo "  ✗ Users: $ACTUAL_USERS (expected $EXPECTED_USERS)"
  VERIFICATION_PASSED=false
fi

if [[ "$ACTUAL_SUBSCRIPTIONS" == "$EXPECTED_SUBSCRIPTIONS" ]]; then
  echo "  ✓ Subscriptions: $ACTUAL_SUBSCRIPTIONS (expected $EXPECTED_SUBSCRIPTIONS)"
else
  echo "  ✗ Subscriptions: $ACTUAL_SUBSCRIPTIONS (expected $EXPECTED_SUBSCRIPTIONS)"
  VERIFICATION_PASSED=false
fi

if [[ "$ACTUAL_REQUESTS" == "$EXPECTED_REQUESTS" ]]; then
  echo "  ✓ ServiceRequests: $ACTUAL_REQUESTS (expected $EXPECTED_REQUESTS)"
else
  echo "  ✗ ServiceRequests: $ACTUAL_REQUESTS (expected $EXPECTED_REQUESTS)"
  VERIFICATION_PASSED=false
fi

if [[ "$ACTUAL_JOBS" == "$EXPECTED_JOBS" ]]; then
  echo "  ✓ Jobs: $ACTUAL_JOBS (expected $EXPECTED_JOBS)"
else
  echo "  ✗ Jobs: $ACTUAL_JOBS (expected $EXPECTED_JOBS)"
  VERIFICATION_PASSED=false
fi

echo ""

if [[ "$VERIFICATION_PASSED" == true ]]; then
  echo "[dev-db-verify] ✓ All verifications passed"
  exit 0
else
  echo "[dev-db-verify] ✗ Verification failed"
  exit 1
fi
