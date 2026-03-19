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

### 2.1.4 - Token Claim Requirements and Validation Rules

Implemented artifacts:

- `backend/application/Identity/TokenClaimNames.cs`
- `backend/application/Identity/TokenClaimsPayload.cs`
- `backend/application/Identity/TokenClaimsValidationIssue.cs`
- `backend/application/Identity/TokenClaimsValidationResult.cs`
- `backend/application/Identity/TokenClaimsValidator.cs`
- `backend/infrastructure.tests/Identity/TokenClaimsValidatorTests.cs`

Required claim baseline:

- `sub` (`TokenClaimNames.Subject`) - required user identifier claim.
- `tenant_id` (`TokenClaimNames.TenantId`) - required tenant identifier claim.
- role claims: `role` or `roles` - at least one required role source.
- `ver` (`TokenClaimNames.TokenVersion`) - required token version claim.

Explicit validation rules:

- Missing `sub` => `missing_subject`.
- Malformed `sub` (not GUID) => `malformed_subject`.
- Missing `tenant_id` => `missing_tenant`.
- Malformed `tenant_id` (not GUID) => `malformed_tenant`.
- Missing role claim inputs (`role` and `roles`) => `missing_roles`.
- Malformed role claim inputs (no non-empty role values after normalization) => `malformed_roles`.
- Missing `ver` => `missing_token_version`.
- Malformed `ver` (non-positive integer) => `malformed_token_version`.

Validation output semantics:

- On success, validator returns `TokenClaimsPayload` (`UserId`, `TenantId`, `Roles`, `TokenVersion`).
- On failure, validator returns structured issues list with `Claim`, `Code`, and `Message` for deterministic API/auth pipeline handling.

### 2.1.5 - Identity and Authorization Non-Goals

Implemented artifact:

- `backend/application/PHASE2_IDENTITY_AUTH_NON_GOALS.md`

Explicitly documented non-goals:

- No cross-tenant self-service switching in Phase 2 user flows.
- No advanced policy automation (rule engines, dynamic policy authoring, adaptive policies).
- No externalized enterprise IAM policy orchestration rollout.
- No tenant entitlement automation beyond the baseline role-permission matrix.
- No broad identity lifecycle automation (self-service provisioning/account linking).

### 2.2.1 - JWT Bearer Authentication Foundation

Implemented artifacts:

- `backend/api/Authentication/JwtAuthenticationOptions.cs`
- `backend/api/Authentication/AuthenticationServiceCollectionExtensions.cs`
- `backend/api/Program.cs`
- `backend/api/appsettings.json`
- `backend/api/appsettings.Development.json`
- `backend/api/appsettings.Local.json`
- `backend/api/appsettings.Production.json`
- `backend/api/appsettings.Local.example.json`
- `backend/api/appsettings.Production.example.json`

Configuration model:

- `Authentication:Jwt:Issuer`
- `Authentication:Jwt:Audience`
- `Authentication:Jwt:SigningKey`

Strict validation defaults:

- `ValidateIssuer = true`
- `ValidateAudience = true`
- `ValidateIssuerSigningKey = true`
- `RequireSignedTokens = true`
- `ValidateLifetime = true`
- `RequireExpirationTime = true`
- `ClockSkew = TimeSpan.Zero`

Environment-aware behavior:

- JWT settings are loaded through environment-specific appsettings plus environment variable overrides.
- HTTPS metadata is required outside `Development` and `Local` environments.
- Startup fails fast when issuer, audience, or signing key is missing/invalid.

### 2.2.2 - Authenticated User Context Abstraction and Infrastructure Adapter

Implemented artifacts:

- `backend/infrastructure/Identity/HttpContextAuthenticatedPrincipalAccessor.cs`
- `backend/infrastructure/DependencyInjection.cs`
- `backend/infrastructure/GTEK.FSM.Backend.Infrastructure.csproj`
- `backend/infrastructure.tests/Identity/HttpContextAuthenticatedPrincipalAccessorTests.cs`

Adapter behavior:

- Infrastructure provides `IAuthenticatedPrincipalAccessor` implementation backed by `IHttpContextAccessor`.
- Adapter reads request claims and validates required identity claims using `TokenClaimsValidator`.
- On valid authenticated context, adapter returns `AuthenticatedPrincipal` (`UserId`, `TenantId`, role membership).
- On unauthenticated or invalid/missing claim state, adapter returns `null` to keep use-case handling explicit.

Dependency injection:

- Registers `IHttpContextAccessor` and scoped `IAuthenticatedPrincipalAccessor` in Infrastructure composition.

### 2.2.3 - Tenant Resolution Order and Explicit Reject Behavior

Implemented artifacts:

- `backend/application/Identity/TenantResolutionPolicy.cs`
- `backend/application/Identity/TenantContextConstants.cs`
- `backend/application/Identity/ITenantContextAccessor.cs`
- `backend/api/Tenancy/TenantResolutionOptions.cs`
- `backend/api/Middleware/TenantResolutionMiddleware.cs`
- `backend/api/Middleware/MiddlewareExtensions.cs`
- `backend/api/Program.cs`
- `backend/infrastructure/Identity/HttpContextTenantContextAccessor.cs`
- `backend/infrastructure/DependencyInjection.cs`
- `backend/infrastructure.tests/Identity/TenantResolutionPolicyTests.cs`
- `backend/infrastructure.tests/Identity/HttpContextTenantContextAccessorTests.cs`
- `backend/api/appsettings.json`
- `backend/api/appsettings.Development.json`
- `backend/api/appsettings.Local.json`
- `backend/api/appsettings.Production.json`
- `backend/api/appsettings.Local.example.json`
- `backend/api/appsettings.Production.example.json`

Resolution behavior:

- Middleware resolves tenant from authenticated request context in strict order:
  1) `tenant_id` claim
  2) configured header fallback (`X-Tenant-Id`) only for configured privileged roles (`Admin` by default)
- Middleware explicitly rejects unresolved/invalid tenant context with deterministic API responses:
  - malformed claim -> `401 MALFORMED_TENANT_CLAIM`
  - unresolved tenant context -> `401 TENANT_CONTEXT_UNRESOLVED`
  - unauthorized header fallback -> `403 TENANT_HEADER_FALLBACK_NOT_ALLOWED`
  - malformed header value -> `400 MALFORMED_TENANT_HEADER`
- On success, resolved tenant id is stored in `HttpContext.Items[ResolvedTenantId]` and exposed via `ITenantContextAccessor`.

### 2.2.4 - Auth Pipeline Bootstrap Endpoints

Implemented artifact:

- `backend/api/Routing/V1RouteGroupExtensions.cs`

Bootstrap routes (under `/api/v1/auth/bootstrap`):

- `GET /authenticated`
  - Returns `200` with principal snapshot (`UserId`, `TenantId`, `ResolvedTenantId`, `Roles`, `Scopes`) when authenticated context is valid.
  - Returns `401 AUTH_UNAUTHORIZED` envelope when authentication context is missing/invalid.
- `GET /forbidden`
  - Returns `200` for admin role.
  - Returns `403 AUTH_FORBIDDEN` envelope for authenticated principals without admin role.
  - Returns `401 AUTH_UNAUTHORIZED` envelope for unauthenticated requests.
- `GET /unauthorized`
  - Deterministic `401 AUTH_UNAUTHORIZED` envelope for client and integration-path verification.

Response standardization:

- All probe endpoints return `ApiResponse<object>` envelopes with `TraceId` for request correlation.

### 2.2.5 - Local/Dev Token Validation Templates and Scripts

Implemented artifacts:

- `backend/api/.env.auth.example`
- `backend/api/scripts/dev-auth-token.sh`
- `backend/api/scripts/dev-auth-bootstrap-check.sh`
- `backend/api/appsettings.Local.example.json`
- `backend/api/appsettings.Docker.example.json`
- `.gitignore`
- `README.md`

Local/dev validation workflow:

- Copy `backend/api/.env.auth.example` to `backend/api/.env.auth.local` (gitignored).
- Set `Authentication__Jwt__Issuer`, `Authentication__Jwt__Audience`, and `Authentication__Jwt__SigningKey` in the local env file.
- Generate local JWTs with configurable role/user/tenant claims via `dev-auth-token.sh`.
- Run `dev-auth-bootstrap-check.sh` to verify expected `401`/`403`/`200` behavior against `/api/v1/auth/bootstrap/*` endpoints.

Secret-safety guardrails:

- Repository templates use explicit `CHANGE_ME` placeholders for signing key secrets.
- Token script refuses to run with placeholder keys and enforces minimum key length.

### 2.3.1 - Policy Names and Authorization Handlers for Role-Scoped Operations

Implemented artifacts:

- `backend/application/Identity/AuthorizationPolicyCatalog.cs`
- `backend/application/Identity/RolePermissionAuthorizer.cs`
- `backend/api/Authorization/PermissionRequirement.cs`
- `backend/api/Authorization/PermissionAuthorizationHandler.cs`
- `backend/api/Authorization/AuthorizationServiceCollectionExtensions.cs`
- `backend/api/Authentication/AuthenticationServiceCollectionExtensions.cs`
- `backend/infrastructure.tests/Identity/AuthorizationPolicyCatalogTests.cs`
- `backend/infrastructure.tests/Identity/RolePermissionAuthorizerTests.cs`

Policy baseline for role-scoped flows:

- `policy.customer.flow` -> `service_requests.write`
- `policy.worker.flow` -> `jobs.write`
- `policy.support.flow` -> `service_requests.write`
- `policy.management.flow` -> `users.write`
- `policy.admin.flow` -> `tenants.write`
- `policy.system.ping` -> `system.ping`

Handler strategy:

- Custom `PermissionRequirement` carries required permission.
- `PermissionAuthorizationHandler` extracts role claims (`ClaimTypes.Role`, `role`, `roles`) and checks permission via `RolePermissionAuthorizer` + `RolePermissionMatrix`.
- Policy registration is centralized through `AddApiAuthorizationPolicies()` and requires authenticated users by default.

### 2.3.2 - Tenant Ownership Checks in Application Use-Case Boundaries

Implemented artifacts:

- `backend/application/Identity/ITenantOwnershipGuard.cs`
- `backend/application/Identity/TenantOwnershipGuard.cs`
- `backend/application/Identity/TenantOwnershipGuardResult.cs`
- `backend/application/DependencyInjection.cs`
- `backend/api/Routing/V1RouteGroupExtensions.cs`
- `backend/infrastructure.tests/Identity/TenantOwnershipGuardTests.cs`

Boundary enforcement behavior:

- `ITenantOwnershipGuard` is registered in Application DI and acts as the canonical boundary check for tenant-scoped operations.
- Guard enforces that requested tenant id must match:
  - authenticated principal tenant (`AuthenticatedPrincipal.TenantId`), and
  - resolved request tenant context (`ITenantContextAccessor`).
- Reject mapping is explicit and deterministic:
  - unauthenticated context -> `401 AUTH_UNAUTHORIZED`
  - unresolved tenant context -> `401 TENANT_CONTEXT_UNRESOLVED`
  - principal/request tenant mismatch -> `403 TENANT_OWNERSHIP_MISMATCH`
  - resolved/request tenant mismatch -> `403 TENANT_CONTEXT_MISMATCH`

Tenant-scoped read/write boundary probes:

- `GET /api/v1/tenant/{tenantId}/ownership-check/read`
- `POST /api/v1/tenant/{tenantId}/ownership-check/write`

Both probe routes call `ITenantOwnershipGuard` before proceeding and return standardized API envelopes.

### 2.3.3 - Privileged Management Guardrails for Cross-Tenant Operations with Audit Hooks

Implemented artifacts:

- `backend/application/Identity/AuthorizationDecisionAuditEvent.cs`
- `backend/application/Identity/IAuthorizationDecisionAuditSink.cs`
- `backend/application/Identity/PrivilegedTenantOperationContracts.cs`
- `backend/application/Identity/IPrivilegedTenantOperationGuard.cs`
- `backend/application/Identity/PrivilegedTenantOperationGuard.cs`
- `backend/application/DependencyInjection.cs`
- `backend/infrastructure/Identity/AuthorizationDecisionAuditLogger.cs`
- `backend/infrastructure/DependencyInjection.cs`
- `backend/api/Routing/V1RouteGroupExtensions.cs`
- `backend/infrastructure.tests/Identity/PrivilegedTenantOperationGuardTests.cs`

Guardrail behavior:

- Explicit cross-tenant management requests are evaluated by `IPrivilegedTenantOperationGuard`.
- Cross-tenant operations require privileged tenant-write capability (`tenants.write`, mapped to Admin baseline role).
- Rejections are explicit (`401`/`403` with deterministic error codes) and all allow/deny decisions emit audit events.

Mandatory audit hook implementation:

- Application defines `IAuthorizationDecisionAuditSink` contract.
- Infrastructure provides `AuthorizationDecisionAuditLogger` with structured fields:
  - `Action`, `Outcome`, `Reason`, `UserId`, `SourceTenantId`, `TargetTenantId`, `OccurredAtUtc`.

Cross-tenant guarded probe:

- `POST /api/v1/management/cross-tenant/{tenantId}/guarded-probe`
- Route invokes `IPrivilegedTenantOperationGuard` before operation success response.

### 2.3.4 - Endpoint-Level Policy Integration and Centralized Registration

Implemented artifacts:

- `backend/api/Program.cs`
- `backend/api/Authentication/AuthenticationServiceCollectionExtensions.cs`
- `backend/api/Routing/V1RouteGroupExtensions.cs`

Composition root registration:

- `AddApiAuthorizationPolicies()` is invoked in `Program.cs` as centralized policy registration in the API composition root.
- `AddApiAuthentication()` now focuses on authentication setup only (JWT bearer).

Endpoint-level policy enforcement:

- `/api/v1/auth/bootstrap/authenticated` -> `policy.system.ping`
- `/api/v1/auth/bootstrap/forbidden` -> `policy.admin.flow`
- `/api/v1/tenant/{tenantId}/ownership-check/read` -> `policy.customer.flow`
- `/api/v1/tenant/{tenantId}/ownership-check/write` -> `policy.worker.flow`
- `/api/v1/management/cross-tenant/{tenantId}/guarded-probe` -> `policy.management.flow`

This ensures route-level policy enforcement is explicit and consistent with the centralized policy catalog and permission handler pipeline.

### 2.3.5 - Authorization Failure Mapping Standards and Payload Semantics

Implemented artifact:

- `backend/api/Authentication/AuthenticationServiceCollectionExtensions.cs`

Failure mapping standard:

- `401 AUTH_UNAUTHORIZED`
  - Triggered by JWT challenge flow when authentication is missing/invalid.
  - Message: `Authentication is required.`
- `403 AUTH_FORBIDDEN`
  - Triggered by JWT forbid flow when authenticated principal lacks required permissions/policy.
  - Message: `You do not have permission to access this resource.`

Payload semantics:

- Both outcomes return standardized `ApiResponse<object>` envelope shape.
- Response includes `TraceId` for correlation and consistent client-side error handling.
- JWT event hooks (`OnChallenge`, `OnForbidden`) centrally enforce this behavior across policy-protected endpoints.

### 2.4.1 - Architecture/Runtime Tests for Required Tenant Context

Implemented artifacts:

- `backend/infrastructure.tests/Architecture/ProtectedEndpointPolicyMetadataTests.cs`
- `backend/infrastructure.tests/Runtime/TenantContextRequiredRuntimeTests.cs`
- `backend/infrastructure.tests/GTEK.FSM.Backend.Infrastructure.Tests.csproj` (API project reference)

Coverage summary:

- Architecture test validates that critical protected endpoints are mapped with explicit authorization policies in endpoint metadata.
- Runtime test validates that authenticated requests without tenant context are blocked by `TenantResolutionMiddleware` before endpoint execution (`401 TENANT_CONTEXT_UNRESOLVED`).

Assertion focus:

- Tenant context is required for protected request paths.
- Missing tenant context prevents request continuation and returns deterministic failure semantics.
