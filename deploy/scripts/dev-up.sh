#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"

cd "$ROOT_DIR"

if [[ ! -f ".env" ]]; then
  echo "[dev-up] .env file not found. Copy .env.example to .env and set values first."
  exit 1
fi

echo "[dev-up] Starting SQL Server and Backend API with Docker Compose..."
docker compose up --build -d

echo "[dev-up] Services are starting. Check status with: docker compose ps"
