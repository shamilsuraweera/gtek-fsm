#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT_DIR"

echo "[dev-logs] Streaming logs for API and SQL Server..."
docker compose logs -f --tail=200 backend-api sqlserver
