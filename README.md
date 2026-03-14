# Vanguard FSM

Vanguard FSM is a multi-tenant Facility Service Management platform with:
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

## Roadmap

See `roadmap.txt` for the phase-by-phase plan.
