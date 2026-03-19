# Phase 2 Identity and Authorization Non-Goals

This document captures explicit non-goals for Phase 2 so implementation remains focused and predictable.

## Scope Boundary

Phase 2 delivers baseline identity, authentication, authorization, and tenant-boundary enforcement only.

## Non-Goals

1. No cross-tenant self-service switching for normal end users.
   - Customer and Worker flows remain fixed to token tenant context.
   - Support/Manager/Admin flows are tenant-scoped by default.
   - Any tenant override remains a future privileged capability with explicit audit controls.

2. No advanced policy automation.
   - No dynamic rule engine for authorization policies.
   - No policy DSL, visual policy editor, or runtime policy authoring.
   - No contextual risk scoring or adaptive policy decisions.

3. No externalized enterprise IAM policy orchestration.
   - No integration with external policy decision points (for example OPA) in Phase 2.
   - No attribute-based access control (ABAC) framework rollout.

4. No tenant entitlement automation beyond baseline role/permission matrix.
   - No per-tenant custom role definitions.
   - No automated feature entitlement negotiation by subscription tier in auth middleware.

5. No broad identity lifecycle automation.
   - No self-service user provisioning workflows.
   - No automated account linking/merging across identity providers.

## Deferred to Later Phases

The above items are candidates for later phases once baseline security and tenant isolation are stable, observable, and regression-tested.
