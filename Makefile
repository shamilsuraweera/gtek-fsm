SHELL := /bin/bash

.PHONY: build build-api build-portal build-mobile \
        run-api run-portal run-mobile run-all \
        dev-db-init dev-db-reset dev-db-seed \
        dev-up dev-down dev-logs dev-reset \
        clean

# Build targets
build:
	dotnet build GTEK.FSM.slnx -c Debug

build-api:
	dotnet build backend/api/GTEK.FSM.Backend.Api.csproj -c Debug

build-portal:
	dotnet build web-portal/GTEK.FSM.WebPortal.csproj -c Debug

build-mobile:
	dotnet build mobile-app/customer-worker/GTEK.FSM.MobileApp.csproj -f net10.0-android -c Debug

# Run targets
run-api:
	./deploy/scripts/run-api-standalone.sh

run-portal:
	./deploy/scripts/run-web-portal.sh

run-mobile:
	./deploy/scripts/run-mobile-app.sh

run-all:
	./deploy/scripts/start-all.sh

# Database targets
dev-db-init:
	./database/scripts/dev-db-init.sh

dev-db-reset:
	./database/scripts/dev-db-reset.sh

dev-db-seed:
	./database/scripts/dev-db-seed.sh

# Docker targets
dev-up:
	./deploy/scripts/dev-up.sh

dev-down:
	./deploy/scripts/dev-down.sh

dev-logs:
	./deploy/scripts/dev-logs.sh

dev-reset:
	./deploy/scripts/dev-reset.sh

# Utility targets
clean:
	find . -type d \( -name 'bin' -o -name 'obj' \) -exec rm -rf {} + 2>/dev/null || true

.DEFAULT_GOAL := build
