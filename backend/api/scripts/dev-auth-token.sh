#!/usr/bin/env bash
set -euo pipefail

# Generates an HS256 JWT for local/dev validation flows.
# Usage examples:
#   ./backend/api/scripts/dev-auth-token.sh
#   ./backend/api/scripts/dev-auth-token.sh --role Support --expires 900
#
# Source precedence:
# 1) backend/api/.env.auth.local (recommended, gitignored)
# 2) environment variables already exported in shell

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
ENV_FILE="$ROOT_DIR/backend/api/.env.auth.local"

if [[ -f "$ENV_FILE" ]]; then
  # shellcheck disable=SC1090
  source "$ENV_FILE"
fi

role="${AUTH_TEST_ROLE:-Admin}"
user_id="${AUTH_TEST_USER_ID:-11111111-1111-1111-1111-111111111111}"
tenant_id="${AUTH_TEST_TENANT_ID:-00000000-0000-0000-0000-000000000001}"
expires_in="${AUTH_TEST_EXPIRES_IN_SECONDS:-3600}"
token_ver="${AUTH_TEST_TOKEN_VERSION:-1}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --role)
      role="$2"
      shift 2
      ;;
    --user-id)
      user_id="$2"
      shift 2
      ;;
    --tenant-id)
      tenant_id="$2"
      shift 2
      ;;
    --expires)
      expires_in="$2"
      shift 2
      ;;
    --token-version)
      token_ver="$2"
      shift 2
      ;;
    *)
      echo "[dev-auth-token] Unknown argument: $1" >&2
      exit 1
      ;;
  esac
done

issuer="${Authentication__Jwt__Issuer:-}"
audience="${Authentication__Jwt__Audience:-}"
signing_key="${Authentication__Jwt__SigningKey:-}"

if [[ -z "$issuer" || -z "$audience" || -z "$signing_key" ]]; then
  echo "[dev-auth-token] Missing required JWT env vars: Authentication__Jwt__Issuer/Audience/SigningKey" >&2
  echo "[dev-auth-token] Create backend/api/.env.auth.local from .env.auth.example and set values." >&2
  exit 1
fi

if [[ ${#signing_key} -lt 32 ]]; then
  echo "[dev-auth-token] Authentication__Jwt__SigningKey must be at least 32 characters." >&2
  exit 1
fi

if [[ "$signing_key" == CHANGE_ME_* ]]; then
  echo "[dev-auth-token] Refusing to use placeholder signing key. Set a real local/dev value." >&2
  exit 1
fi

base64url() {
  openssl base64 -A | tr '+/' '-_' | tr -d '='
}

now="$(date +%s)"
exp="$((now + expires_in))"

header='{"alg":"HS256","typ":"JWT"}'
payload="{\"sub\":\"$user_id\",\"tenant_id\":\"$tenant_id\",\"role\":\"$role\",\"ver\":\"$token_ver\",\"iss\":\"$issuer\",\"aud\":\"$audience\",\"iat\":$now,\"nbf\":$now,\"exp\":$exp,\"jti\":\"$now-$user_id\"}"

header_b64="$(printf '%s' "$header" | base64url)"
payload_b64="$(printf '%s' "$payload" | base64url)"
unsigned_token="$header_b64.$payload_b64"
signature="$(printf '%s' "$unsigned_token" | openssl dgst -sha256 -hmac "$signing_key" -binary | base64url)"

echo "$unsigned_token.$signature"
