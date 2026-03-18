#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT_DIR"

echo "[dev-reset] Stopping services and removing volumes for a clean local reset..."
docker compose down -v --remove-orphans

echo "[dev-reset] Reset complete. Start fresh with ./deploy/scripts/dev-up.sh"
