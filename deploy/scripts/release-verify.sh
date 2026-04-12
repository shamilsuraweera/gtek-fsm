#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 1 ]]; then
  echo "Usage: $0 <environment>"
  echo "Example: $0 staging"
  exit 1
fi

ENVIRONMENT="$1"
ENV_FILE=".env.${ENVIRONMENT}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Error: environment file '$ENV_FILE' not found."
  echo "Create it from .env.${ENVIRONMENT}.example and fill required values first."
  exit 1
fi

set -a
source "$ENV_FILE"
set +a

API_SCHEME="${API_SCHEME:-http}"
API_HOST="${API_HOST:-localhost}"
API_PORT="${API_PORT:-5000}"
HEALTH_URL="${API_SCHEME}://${API_HOST}:${API_PORT}/health"
READY_URL="${API_SCHEME}://${API_HOST}:${API_PORT}/health/ready"

echo "[release-verify] Environment: ${ENVIRONMENT}"
echo "[release-verify] Verifying ${HEALTH_URL}"
curl -fsS "$HEALTH_URL" >/dev/null

echo "[release-verify] Verifying ${READY_URL}"
curl -fsS "$READY_URL" >/dev/null

echo "[release-verify] Release verification succeeded."
