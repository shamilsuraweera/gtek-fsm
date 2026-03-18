#!/usr/bin/env bash
set -euo pipefail

# Seed placeholder runner for local development.
# Current phase (0.7.3) does not define business schema, so this script
# validates seed file ordering and prepares execution flow.
#
# Usage:
#   ./database/scripts/dev-db-seed.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SEEDS_DIR="$ROOT_DIR/database/seeds"

echo "[dev-db-seed] Listing seed files..."

mapfile -t SEED_FILES < <(find "$SEEDS_DIR" -maxdepth 1 -type f -name "*.sql" | sort)

if [[ ${#SEED_FILES[@]} -eq 0 ]]; then
  echo "[dev-db-seed] No seed SQL files found."
  exit 0
fi

for seed_file in "${SEED_FILES[@]}"; do
  echo "[dev-db-seed] Prepared: $(basename "$seed_file")"
done

echo "[dev-db-seed] Seed pipeline is ready."
echo "[dev-db-seed] Execution hook will be activated in the next phase once schema tables exist."
