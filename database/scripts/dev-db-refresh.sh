#!/usr/bin/env bash
set -euo pipefail

# Comprehensive database refresh: reset + init + seed + verify schema and data.
# Usage:
#   ./database/scripts/dev-db-refresh.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

echo "=========================================="
echo "[dev-db-refresh] Starting comprehensive database refresh"
echo "=========================================="
echo ""

echo "[STEP 1/4] Resetting database..."
"$ROOT_DIR/database/scripts/dev-db-reset.sh"
echo "✓ Database reset complete"
echo ""

echo "[STEP 2/4] Verifying schema creation..."
"$ROOT_DIR/database/scripts/dev-db-verify.sh" || echo "  (seed data not yet populated - proceeding)"
echo ""

echo "[STEP 3/4] Applying seed data..."
"$ROOT_DIR/database/scripts/dev-db-seed.sh"
echo "✓ Seed data applied"
echo ""

echo "[STEP 4/4] Verifying seed data populations..."
if "$ROOT_DIR/database/scripts/dev-db-verify.sh"; then
  echo ""
  echo "=========================================="
  echo "✓ Database refresh complete and verified"
  echo "=========================================="
else
  echo ""
  echo "=========================================="
  echo "✗ Database verification failed"
  echo "=========================================="
  exit 1
fi
