#!/usr/bin/env bash
set -euo pipefail

# Seed runner for local development.
# Applies ordered SQL seed scripts with idempotent history tracking so
# repeated runs do not duplicate seed effects.
#
# Usage:
#   ./database/scripts/dev-db-seed.sh

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
  echo "[dev-db-seed] sqlcmd not found. Install mssql-tools18 to run seed scripts."
  exit 1
fi

if [[ -z "$SQL_PASSWORD" ]]; then
  echo "[dev-db-seed] SA_PASSWORD/SQL_PASSWORD is not set."
  echo "[dev-db-seed] Set it in .env or export in your shell."
  exit 1
fi

echo "[dev-db-seed] Target: ${SQL_HOST},${SQL_PORT} / ${SQL_DATABASE}"
echo "[dev-db-seed] Discovering seed files..."

mapfile -t SEED_FILES < <(find "$SEEDS_DIR" -maxdepth 1 -type f -name "*.sql" | sort)

if [[ ${#SEED_FILES[@]} -eq 0 ]]; then
  echo "[dev-db-seed] No seed SQL files found."
  exit 0
fi

echo "[dev-db-seed] Ensuring seed history table exists..."
sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -Q "
SET NOCOUNT ON;
IF OBJECT_ID(N'dbo.__SeedHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__SeedHistory
    (
        ScriptName NVARCHAR(260) NOT NULL,
        AppliedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF___SeedHistory_AppliedAtUtc DEFAULT SYSUTCDATETIME(),
        AppliedBy NVARCHAR(128) NULL,
        CONSTRAINT PK___SeedHistory PRIMARY KEY (ScriptName)
    );
END;
"

for seed_file in "${SEED_FILES[@]}"; do
  script_name="$(basename "$seed_file")"
  script_name_sql="${script_name//\'/\'\'}"

  applied_count="$(sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(1) FROM dbo.__SeedHistory WHERE ScriptName = N'${script_name_sql}';" | tr -d '[:space:]')"

  if [[ "$applied_count" != "0" ]]; then
    echo "[dev-db-seed] Skipping (already applied): $script_name"
    continue
  fi

  echo "[dev-db-seed] Applying: $script_name"
  sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -i "$seed_file"

  sqlcmd -S "${SQL_HOST},${SQL_PORT}" -U "$SQL_USER" -P "$SQL_PASSWORD" -C -d "$SQL_DATABASE" -b -Q "
SET NOCOUNT ON;
INSERT INTO dbo.__SeedHistory (ScriptName, AppliedBy)
VALUES (N'${script_name_sql}', N'dev-db-seed.sh');
"

  echo "[dev-db-seed] Applied: $script_name"
done

echo "[dev-db-seed] Seed execution complete."
