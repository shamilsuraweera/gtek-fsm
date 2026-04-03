#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT_DIR"

# Load compose variables from .env so docker compose does not warn about missing substitutions.
if [[ -f "$ROOT_DIR/.env" ]]; then
	set -o allexport
	# shellcheck source=/dev/null
	source "$ROOT_DIR/.env"
	set +o allexport
fi

echo "[dev-logs] Streaming logs for API and SQL Server..."
docker compose logs -f --tail=200 backend-api sqlserver
