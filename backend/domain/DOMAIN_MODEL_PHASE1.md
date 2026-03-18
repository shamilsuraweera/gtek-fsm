# Phase 1 Domain Model - Aggregate Roots and Ownership Boundaries

This document defines the initial aggregate roots for Phase 1.1.1 and their ownership boundaries.

## Aggregate Roots

### Tenant
- Aggregate root for tenant-level ownership and isolation.
- Owns:
  - Tenant identity (`Id`, `Code`, `Name`)
  - Active subscription linkage (`ActiveSubscriptionId`)
- Boundary rules:
  - Cross-tenant references are not allowed.
  - A tenant can have multiple users, requests, jobs, and subscriptions.

### User
- Aggregate root for platform actors inside a tenant.
- Owns:
  - User identity (`Id`, `ExternalIdentity`)
  - Display profile (`DisplayName`)
  - Tenant ownership (`TenantId`)
- Boundary rules:
  - A user belongs to exactly one tenant.
  - User cannot be shared across tenants.

### ServiceRequest
- Aggregate root for customer-originated work intake.
- Owns:
  - Request identity (`Id`)
  - Tenant ownership (`TenantId`)
  - Customer linkage (`CustomerUserId`)
  - Active job linkage (`ActiveJobId`)
- Boundary rules:
  - Request belongs to exactly one tenant.
  - Request creator customer must be from the same tenant (enforced at orchestration/policy layer in Phase 2).

### Job
- Aggregate root for executable work derived from a service request.
- Owns:
  - Job identity (`Id`)
  - Tenant ownership (`TenantId`)
  - Request linkage (`ServiceRequestId`)
  - Worker assignment (`AssignedWorkerUserId`)
- Boundary rules:
  - Job belongs to exactly one tenant.
  - Job references one service request.
  - Assigned worker must belong to same tenant (enforced by policy layer).

### Subscription
- Aggregate root for tenant commercial plan boundaries.
- Owns:
  - Subscription identity (`Id`)
  - Tenant ownership (`TenantId`)
  - Plan details (`PlanCode`, `StartsOnUtc`, `EndsOnUtc`)
- Boundary rules:
  - Subscription belongs to exactly one tenant.
  - Tenant may switch active subscription over time.

## Ownership Graph (Phase 1)

- `Tenant` is top ownership boundary for all business aggregates.
- `User`, `ServiceRequest`, `Job`, and `Subscription` carry `TenantId` explicitly.
- No aggregate root directly mutates another aggregate root.
- Cross-aggregate consistency is coordinated in Application layer use cases.

## Notes

- This is the minimal phase-1 aggregate shape for schema and persistence work.
- Value objects, lifecycle enums, and domain events are introduced in 1.1.2, 1.1.3, and 1.1.5.
