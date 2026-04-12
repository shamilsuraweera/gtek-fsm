#!/usr/bin/env bash

set -euo pipefail

if [[ $# -ne 2 ]]; then
  echo "Usage: $0 <environment> <backend_api_image_tag>"
  echo "Example: $0 staging ghcr.io/org/gtek-fsm-backend-api:2026.03.15.1"
  exit 1
fi

ENVIRONMENT="$1"
BACKEND_IMAGE_TAG="$2"
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

echo "[release-deploy] Environment: ${ENVIRONMENT}"
echo "[release-deploy] Backend API image: ${BACKEND_IMAGE_TAG}"
echo "[release-deploy] Compose env file: ${ENV_FILE}"

export BACKEND_API_IMAGE="${BACKEND_IMAGE_TAG}"

docker compose --env-file "$ENV_FILE" pull backend-api
docker compose --env-file "$ENV_FILE" up -d sqlserver backend-api

echo "[release-deploy] Waiting for health endpoint: ${HEALTH_URL}"
for _ in $(seq 1 60); do
  if curl -fsS "$HEALTH_URL" >/dev/null 2>&1; then
    echo "[release-deploy] Health check passed."
    echo "[release-deploy] Release deployment completed."
    exit 0
  fi
  sleep 2
done

echo "[release-deploy] Health check failed after timeout."
echo "[release-deploy] Investigate logs with: docker compose --env-file ${ENV_FILE} logs --tail=200 backend-api"
exit 1
