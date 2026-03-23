# Phase 6 Completion Criteria and Phase 7 Handoff Prerequisites

## Scope

This document closes Phase 6 (Mobile Field Shell) and defines explicit entry prerequisites for Phase 7 mobile feature expansion.

## Phase 6 Completion Criteria

Phase 6 is considered complete when all criteria below are satisfied.

### 1. Tracker Completion

- Roadmap items `6.1.1` through `6.5.5` are marked complete in `tracker.txt`.

### 2. Mobile Foundation and Architecture Baseline

- Mobile app shell is in place for Customer and Worker sections.
- Theme support (light/dark) is operational and aligned with shared design-token direction.
- Session, tenant, and theme state containers are initialized and available at app scope.
- Placeholder route topology is fully wired for Home, Requests, Jobs, Profile, and Settings.
- Environment-based API endpoint configuration differentiates local and non-local behavior.

### 3. Startup and Deployment Baseline

- Mobile startup script supports Linux Android flow and detects platform/tooling requirements.
- Startup flow is idempotent and recoverable from common failure conditions:
  - emulator unavailable,
  - local API port conflicts,
  - stale build cache/intermediate state.
- Optional backend bootstrap path is available for end-to-end local mobile testing.

### 4. Connectivity and Tenant-Safety Baseline

- Authenticated API connectivity path is in place for mobile flows.
- Tenant context initialization from identity token is enforced for app startup context.
- Mobile state recovery behavior covers loading/stale/retry/partial-failure paths.
- Connectivity diagnostics and recovery state/services are available for local troubleshooting.

### 5. Customer and Worker Shell Readiness

- Customer shell includes dashboard, request list/detail, status tracking, and profile editing.
- Worker shell includes job list/detail workspace, assignment acceptance, and status updates.
- Role-gated navigation is enforced from token-derived role context.
- Quick-action pathways are available for high-frequency field and customer operations.
- Requests/jobs screens are wired to live API query endpoints with resilient fallback behavior.

### 6. Security Hardening Baseline

- Sensitive UI state is masked while app is backgrounded.
- Sensitive state is cleared on logout.
- Token expiry is validated on startup/resume lifecycle transitions.
- Security lifecycle behavior is centrally implemented and reusable.

### 7. Test and Validation Baseline

- Baseline mobile tests exist for:
  - role-gated navigation behavior,
  - authenticated identity/tenant context initialization assumptions,
  - JWT expiry parsing and token lifecycle assumptions.
- `mobile-app/customer-worker.tests` is included in solution and executes successfully.
- Android build validation for `net10.0-android` completes successfully (warnings allowed; no blocking errors).

### 8. Documentation Baseline

- Mobile development runbook exists with setup, common issues, and troubleshooting paths.
- Main repository docs include direct links to mobile runbook artifacts.

## Phase 7 Handoff Prerequisites (Explicit)

Phase 7 mobile feature expansion may begin only when all prerequisites below are true.

### A. Security and Tenancy Invariants to Preserve

- New mobile flows must continue to initialize tenant context from authenticated identity claims.
- No role-specific screen/action may bypass role-gated navigation checks.
- Token expiry and logout-clearing behavior must remain active for all new authenticated flows.
- Sensitive data handling in background/resume must be preserved for newly added screens.

### B. Feature Design Constraints for Expansion

- New pages must integrate into existing shell patterns rather than introducing parallel shell structures.
- Customer and Worker experiences must remain explicitly role-segmented.
- API consumption should use existing resilient query/service patterns (envelope tolerance + fallback strategy) unless explicitly replaced by a documented shared alternative.
- Any new high-frequency workflow should include a quick-action pathway consistent with Phase 6 interaction patterns.

### C. Validation Requirements for New Mobile Features

- For each new role-aware workflow, add role allow/deny test coverage.
- For each new auth-dependent flow, include token/tenant context assumptions in tests.
- For each new API-integrated screen, validate failure-path behavior (loading, stale, retry, partial failure) in tests or deterministic verification scripts.
- Maintain successful Android build validation in CI/local checkpoints.

### D. Operational and Developer Experience Prerequisites

- `deploy/scripts/run-mobile-app.sh` remains the canonical mobile startup path and must stay idempotent.
- Recovery options documented in mobile runbook must be kept current as startup script behavior evolves.
- Mobile troubleshooting guidance must be updated whenever new feature dependencies are introduced (SDKs, services, environment keys, or required tooling).

### E. Handoff Inputs Required from Phase 6

- Baseline test project and passing test run evidence.
- Build validation evidence for Android target.
- Current runbook and startup script docs as references for onboarding.
- Tracker state showing all Phase 6 tasks complete.

## Handoff Summary

Phase 6 establishes a stable, security-aware mobile shell for Customer and Worker experiences, with resilient startup and baseline test coverage. Phase 7 mobile feature expansion can proceed with these criteria as non-negotiable guardrails for reliability, tenancy safety, and operational consistency.
