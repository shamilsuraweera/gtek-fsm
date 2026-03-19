# Phase 2 Planning - Identity, Access, and Tenant Boundaries

This document defines the implementation planning baseline for Phase 2.

## Objective

Establish secure and testable identity and authorization foundations that enforce tenant isolation by default across API, application workflows, and persistence paths.

## Inputs from Phase 1

- Tenant-scoped aggregate ownership and repository filtering are already in place.
- `IdentityValue` and `User.ExternalIdentity` are available for identity mapping.
- Query-path tenant safety has focused repository-level validation.
- Migration/seed scripts are stable for repeatable local and dev setup.

## Constraints

- Keep clean architecture boundaries from `config/architecture-rules.json`.
- Keep tenancy rules from `config/tenancy-approach.json`:
  - `tenant_id` claim is primary tenant context source.
  - Header fallback is restricted to privileged/internal flows.
  - Missing tenant context must reject requests.
- Preserve existing ownership boundaries introduced in Phase 1.

## Workstreams

### 1. Identity Contracts

- Define principal contract used by Application (`UserId`, `TenantId`, roles/scopes).
- Define required token claims and validation behavior.
- Define role-to-permission mapping baseline for current participant roles.

### 2. Authentication

- Configure JWT bearer validation with strict issuer/audience/signature checks.
- Add environment-safe configuration strategy for local/dev/prod.
- Expose authenticated principal and tenant context via abstractions, not direct controller coupling.

### 3. Authorization

- Define policy catalog and handler strategy.
- Enforce role + tenant checks at use-case boundaries.
- Standardize `401` and `403` mapping and response payload shape.

### 4. Tenant Isolation Hardening

- Add tests for missing tenant, tenant mismatch, and claim tampering.
- Add regression tests ensuring auth context and repository tenant filters align.
- Add decision-point audit fields for investigation and compliance.

### 5. Readiness Gate

- Execute security rehearsal for unauthenticated, unauthorized, forbidden, and valid access paths.
- Confirm role matrix behavior and cross-tenant denial behavior.
- Publish Phase 2 completion and Phase 3 handoff prerequisites.

## Sequencing

1. Identity contracts and claim requirements.
2. Authentication configuration and principal context abstraction.
3. Authorization policies and endpoint wiring.
4. Tenant hardening tests and audit observability.
5. Final readiness rehearsal and completion documentation.

## Done Criteria

Phase 2 is complete when all tracker items `2.1.1` through `2.5.5` are marked done, with automated tests covering critical allow/deny and tenant-isolation paths.

## Out of Scope for Phase 2

- Tenant self-service onboarding automation.
- Per-tenant dedicated databases.
- Advanced entitlement automation beyond baseline role/policy enforcement.

## Progress Notes

### 2.1.1 - Identity Provider Boundary Contract

Implemented artifacts:

- `backend/application/Identity/IdentityProviderBoundaryContract.cs`
- `backend/application/Identity/IdentityProviderBoundaryMapper.cs`
- `backend/infrastructure.tests/Identity/IdentityProviderBoundaryMapperTests.cs`

Mapping semantics:

- Boundary contract requires `Provider`, `Subject`, and `Issuer`.
- `Provider` is normalized to lowercase for stable identity matching.
- Canonical persisted `User.ExternalIdentity` format remains `provider:subject` via `IdentityValue.ToString()`.
- Mapper converts from boundary contract to domain `IdentityValue` and persisted external identity string.
- Mapper also converts persisted `User.ExternalIdentity` back to boundary contract with caller-supplied `Issuer` context.
- Legacy external identity values without a `:` separator are preserved as subject-only and mapped with provider `legacy` for backward compatibility with existing data.

### 2.1.2 - Authenticated Principal Model

Implemented artifacts:

- `backend/application/Identity/AuthenticatedPrincipal.cs`
- `backend/application/Identity/IAuthenticatedPrincipalAccessor.cs`
- `backend/infrastructure.tests/Identity/AuthenticatedPrincipalTests.cs`

Model semantics:

- `AuthenticatedPrincipal` contains application-level identity context only: `UserId`, `TenantId`, `Roles`, `Scopes`.
- Model is transport-agnostic and has no dependency on HTTP, JWT claims APIs, or framework-specific principal types.
- `Roles` and `Scopes` are normalized into case-insensitive sets, trimming whitespace and deduplicating entries.
- Helper methods `IsInRole(...)` and `HasScope(...)` provide consistent authorization checks in application use cases.
- `IAuthenticatedPrincipalAccessor` defines a single application abstraction (`GetCurrent`) for retrieving the current principal from any transport adapter.

### 2.1.3 - Role and Permission Matrix

Implemented artifacts:

- `backend/application/Identity/Permissions.cs`
- `backend/application/Identity/RolePermissionMatrix.cs`
- `backend/infrastructure.tests/Identity/RolePermissionMatrixTests.cs`

Matrix baseline:

- All roles include `system.ping` to support the current API reachability surface.
- Permission catalog covers current domain use-case surfaces prepared in Phase 1/2:
  - tenants, users, service requests, jobs, subscriptions.
- Role differentiation is explicit:
  - `Guest`: ping-only.
  - `Customer`: service request read/write + job read.
  - `Worker`: service request read + job read/write.
  - `Support`: operational read/write for requests/jobs with read-only tenant/user/subscription visibility.
  - `Manager`: support capabilities plus user and subscription write permissions.
  - `Admin`: full baseline permissions including tenant write.

Implementation note:

- Matrix returns case-insensitive permission sets and provides `HasPermission(role, permission)` for policy wiring in upcoming Phase 2 tasks.
