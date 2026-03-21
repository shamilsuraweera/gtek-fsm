# GTEK FSM

GTEK FSM is a multi-tenant Facility Service Management platform with:

- Mobile app for Customers and Workers
- Web portal for Management and Customer Care
- Shared .NET and MS SQL backend

## Current Status

This repository is in early scaffolding stage (Phase 0).

## Repository Structure

- `backend/` - API and backend services
- `web-portal/` - management and customer care web app
- `mobile-app/` - customer and worker mobile app
- `shared/` - shared contracts, models, and design assets
- `database/` - database assets and migration strategy
- `deploy/` - deployment and runtime orchestration assets
- `config/` - environment and configuration templates

## Project Boundaries

- `backend/domain/` - domain entities, value objects, and domain rules
- `backend/application/` - use cases and application orchestration
- `backend/infrastructure/` - persistence and external integrations
- `backend/api/` - HTTP host and transport layer
- `shared/contracts/` - shared API and cross-client contracts
- `web-portal/customer-care/` and `web-portal/management/` - web client areas
- `mobile-app/customer-worker/` - shared mobile client area for customer and worker flows

## Naming Conventions

Naming conventions are defined in `config/naming-conventions.json`.

## Tenancy and Architecture Rules

- Tenancy approach is defined in `config/tenancy-approach.json`.
- Baseline layering and dependency rules are defined in `config/architecture-rules.json`.

## Roadmap

See `roadmap.txt` for the phase-by-phase plan.

## Local Auth Token Validation (Phase 2)

- Copy `backend/api/.env.auth.example` to `backend/api/.env.auth.local` and set local values.
- Start API with matching JWT env values (or local appsettings overrides).
- Generate a token for local/dev checks:
  - `./backend/api/scripts/dev-auth-token.sh`
- Run bootstrap auth probe checks (`401`, `403`, `200` paths):
  - `./backend/api/scripts/dev-auth-bootstrap-check.sh`

Notes:

- `backend/api/.env.auth.local` is gitignored.
- Do not commit real signing keys to repository-tracked files.
