# Phase 2 Completion Criteria and Phase 3 Handoff Prerequisites

## Scope

This document closes Phase 2 (Identity, Access, and Tenant Boundaries) and defines explicit entry prerequisites for Phase 3 (Core Service Platform).

## Phase 2 Completion Criteria

Phase 2 is considered complete when all criteria below are satisfied.

### 1. Tracker Completion

- Roadmap items `2.1.1` through `2.5.5` are marked complete in `tracker.txt`.

### 2. Identity and Claim Validation Baseline

- Identity provider boundary contract and mapping behavior are implemented.
- Authenticated principal model is available through application abstraction (`IAuthenticatedPrincipalAccessor`).
- Required token claims are validated with deterministic failure reasons for malformed/missing claims.

### 3. Authentication and Tenant Resolution Baseline

- JWT bearer authentication is configured with strict validation defaults.
- Tenant resolution is enforced (`tenant_id` claim primary) with reject behavior for unresolved tenant context.
- `401` and `403` mappings are consistent across auth pipeline outcomes.

### 4. Authorization and Policy Enforcement Baseline

- Policy catalog and endpoint-level policy wiring are active for critical role-based flows.
- Role-permission matrix behavior is validated via integration tests for allow/deny outcomes.

### 5. Tenant Boundary Enforcement Baseline

- Cross-tenant access attempts are explicitly denied for non-privileged roles.
- Privileged cross-tenant exceptions are constrained to authorized role/permission paths.
- Tenant ownership checks are enforced for tenant-scoped routes.

### 6. Observability and Diagnostics Baseline

- Structured authorization audit fields are present (`UserId`, `SourceTenantId`, `TargetTenantId`, `Action`, `Outcome`, `Reason`, `OccurredAtUtc`).
- Security troubleshooting runbook is available for local/dev diagnostics.

### 7. Readiness Gate Test Evidence

The readiness gate is backed by passing test suites:

- `EndToEndAuthenticatedRequestFlowTests` (2.5.1)
- `RoleAccessMatrixIntegrationTests` (2.5.2)
- `CrossTenantAccessDenialIntegrationTests` (2.5.3)
- `SecurityRehearsalIntegrationTests` (2.5.4)

## Phase 3 Handoff Prerequisites (Explicit)

Phase 3 service workflow implementation may begin only when all prerequisites below are true.

### A. Security Invariants to Preserve

- No service workflow path may bypass authenticated principal accessors.
- Tenant context must be resolved before any tenant-scoped workflow execution.
- Cross-tenant workflow operations must remain guarded by privileged operation checks.
- Endpoint and use-case authorization must continue to produce deterministic `401`/`403` outcomes.

### B. Application Design Constraints for Phase 3

- New service workflow handlers must consume `IAuthenticatedPrincipalAccessor` and `ITenantContextAccessor` abstractions rather than raw transport primitives.
- Tenant ownership checks must occur at use-case boundaries before repository mutation/query operations.
- New routes must be mapped to existing or explicitly added policies in the centralized policy catalog.

### C. Testing Requirements for Phase 3 Entry

- For each new workflow endpoint, add at least one allow case and one deny case.
- For each tenant-scoped mutation endpoint, add at least one tenant-mismatch forbidden test.
- Maintain security rehearsal coverage as Phase 3 routes are introduced.

### D. Operational Prerequisites

- Local/dev environment token validation configuration remains documented and usable.
- Security runbook remains current with any new policy or claim behavior introduced in Phase 3.

## Handoff Summary

Phase 2 establishes a production-shaped security envelope for identity, authorization, and tenant boundaries. Phase 3 may now focus on service workflow implementation, with the above security constraints treated as non-negotiable guardrails.
