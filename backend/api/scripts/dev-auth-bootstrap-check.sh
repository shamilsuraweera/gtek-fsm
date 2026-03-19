#!/usr/bin/env bash
set -euo pipefail

# Calls auth bootstrap probe endpoints with generated test tokens.
# Requires: backend/api/.env.auth.local (recommended) and API running locally.
# Usage:
#   ./backend/api/scripts/dev-auth-bootstrap-check.sh

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
ENV_FILE="$ROOT_DIR/backend/api/.env.auth.local"
TOKEN_SCRIPT="$ROOT_DIR/backend/api/scripts/dev-auth-token.sh"

if [[ -f "$ENV_FILE" ]]; then
  # shellcheck disable=SC1090
  source "$ENV_FILE"
fi

api_base="${API_BASE_URL:-http://localhost:5023}"

if [[ ! -x "$TOKEN_SCRIPT" ]]; then
  echo "[dev-auth-bootstrap-check] Token script is not executable: $TOKEN_SCRIPT" >&2
  exit 1
fi

print_call() {
  local label="$1"
  local path="$2"
  local token="${3:-}"

  echo
  echo "[$label] GET $api_base$path"

  if [[ -n "$token" ]]; then
    curl -sS -i -H "Authorization: Bearer $token" "$api_base$path"
  else
    curl -sS -i "$api_base$path"
  fi
}

admin_token="$($TOKEN_SCRIPT --role Admin)"
support_token="$($TOKEN_SCRIPT --role Support)"

print_call "unauthorized-probe" "/api/v1/auth/bootstrap/unauthorized"
print_call "authenticated-admin" "/api/v1/auth/bootstrap/authenticated" "$admin_token"
print_call "forbidden-with-support" "/api/v1/auth/bootstrap/forbidden" "$support_token"
print_call "forbidden-with-admin" "/api/v1/auth/bootstrap/forbidden" "$admin_token"

echo
echo "[dev-auth-bootstrap-check] Completed auth bootstrap probe run."
