# Phase 1.5.1 - ERD and Data Dictionary

This artifact provides:

- A complete Phase 1 Entity Relationship Diagram (ERD).
- A data dictionary for all Phase 1 entities with key field meanings.
- Relationship and tenant-safety notes tied to the current migration baseline.

Schema baseline source:

- `backend/infrastructure/Persistence/Migrations/20260318161457_Phase1InitialSchema.cs`

## ERD (Mermaid)

````mermaid
erDiagram
    Tenants ||--o{ Users : "owns"
    Tenants ||--o{ ServiceRequests : "owns"
    Tenants ||--o{ Jobs : "owns"
    Tenants ||--o{ Subscriptions : "owns"

    Users ||--o{ ServiceRequests : "customer creates"
    Users ||--o{ Jobs : "worker assigned"

    ServiceRequests ||--o{ Jobs : "request has jobs"
    Jobs o|--|| ServiceRequests : "active job (optional)"

    Subscriptions o|--|| Tenants : "active subscription (optional)"

    Tenants {
      uniqueidentifier Id PK
      nvarchar Code UK
      nvarchar Name
      uniqueidentifier ActiveSubscriptionId FK nullable
      datetime2 CreatedAtUtc
      datetime2 UpdatedAtUtc
      bit IsDeleted
    }

    Users {
      uniqueidentifier Id PK
      uniqueidentifier TenantId FK
      nvarchar ExternalIdentity UK_tenant
      nvarchar DisplayName
      datetime2 CreatedAtUtc
      datetime2 UpdatedAtUtc
      bit IsDeleted
    }

    ServiceRequests {
      uniqueidentifier Id PK
      uniqueidentifier TenantId FK
      uniqueidentifier CustomerUserId FK
      nvarchar Title
      tinyint Status
      uniqueidentifier ActiveJobId FK nullable
      datetime2 CreatedAtUtc
      datetime2 UpdatedAtUtc
      bit IsDeleted
    }

    Jobs {
      uniqueidentifier Id PK
      uniqueidentifier TenantId FK
      uniqueidentifier ServiceRequestId FK
      tinyint AssignmentStatus
      uniqueidentifier AssignedWorkerUserId FK nullable
      datetime2 CreatedAtUtc
      datetime2 UpdatedAtUtc
      bit IsDeleted
    }

    Subscriptions {
      uniqueidentifier Id PK
      uniqueidentifier TenantId FK
      nvarchar PlanCode
      datetime2 StartsOnUtc
      datetime2 EndsOnUtc nullable
      datetime2 CreatedAtUtc
      datetime2 UpdatedAtUtc
      bit IsDeleted
    }
```text

## Relationship Map

- `Users.TenantId` -> `Tenants.Id`
- `ServiceRequests.TenantId` -> `Tenants.Id`
- `Jobs.TenantId` -> `Tenants.Id`
- `Subscriptions.TenantId` -> `Tenants.Id`
- `ServiceRequests (TenantId, CustomerUserId)` -> `Users (TenantId, Id)`
- `Jobs (TenantId, ServiceRequestId)` -> `ServiceRequests (TenantId, Id)`
- `Jobs (TenantId, AssignedWorkerUserId)` -> `Users (TenantId, Id)` (optional)
- `ServiceRequests (TenantId, ActiveJobId)` -> `Jobs (TenantId, Id)` (optional)
- `Tenants (Id, ActiveSubscriptionId)` -> `Subscriptions (TenantId, Id)` (optional)

Tenant-safety enforcement is implemented via composite keys and foreign keys including `TenantId`.

## Data Dictionary

### Table: `Tenants`

Purpose:

- Root entity representing tenant boundary and ownership scope.

| Field | Type | Required | Key/Constraint | Meaning |
| --- | --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | `PK_Tenants` | Stable tenant identifier and ownership boundary key used by all tenant-owned aggregates. |
| `Code` | `nvarchar(32)` | Yes | `UQ_Tenants_Code` | Human-manageable unique tenant code used for lookup and provisioning contexts. |
| `Name` | `nvarchar(120)` | Yes | - | Display/business name for tenant. |
| `ActiveSubscriptionId` | `uniqueidentifier` | No | FK composite to `Subscriptions` | Optional pointer to currently active commercial subscription for this tenant. |
| `CreatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC timestamp when tenant row was created. |
| `UpdatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC timestamp for latest persistence update. |
| `IsDeleted` | `bit` | Yes | default `0` | Soft-delete marker; filtered from normal reads when `true`. |

### Table: `Users`

Purpose:

- Identity/profile entity for actors within a tenant.

| Field | Type | Required | Key/Constraint | Meaning |
| --- | --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | `PK_Users` | Stable user identifier inside tenant scope. |
| `TenantId` | `uniqueidentifier` | Yes | FK to `Tenants`, `AK_Users_TenantId_Id` | Tenant ownership key; prevents cross-tenant identity association. |
| `ExternalIdentity` | `nvarchar(128)` | Yes | `UQ_Users_TenantId_ExternalIdentity` | External auth/provider identity key unique within tenant. |
| `DisplayName` | `nvarchar(120)` | Yes | `IX_Users_TenantId_DisplayName` | User-facing name used in operations and UI views. |
| `CreatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row creation timestamp. |
| `UpdatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row update timestamp. |
| `IsDeleted` | `bit` | Yes | default `0` | Soft-delete marker for logical removal. |

### Table: `ServiceRequests`

Purpose:

- Customer-originated request intake and lifecycle tracking.

| Field | Type | Required | Key/Constraint | Meaning |
| --- | --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | `PK_ServiceRequests` | Stable request identifier. |
| `TenantId` | `uniqueidentifier` | Yes | FK to `Tenants`, `AK_ServiceRequests_TenantId_Id` | Tenant ownership discriminator for request and child references. |
| `CustomerUserId` | `uniqueidentifier` | Yes | FK composite to `Users (TenantId, Id)` | User who created/requested service; enforced to same tenant. |
| `Title` | `nvarchar(180)` | Yes | - | Short request descriptor used in queues and UI. |
| `Status` | `tinyint` | Yes | default `0`, `IX_ServiceRequests_TenantId_Status` | Request lifecycle state (`ServiceRequestStatus` enum). |
| `ActiveJobId` | `uniqueidentifier` | No | FK composite to `Jobs`, `UQ_ServiceRequests_TenantId_ActiveJobId` | Optional currently active linked job for request execution. |
| `CreatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row creation timestamp. |
| `UpdatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row update timestamp. |
| `IsDeleted` | `bit` | Yes | default `0` | Soft-delete marker for logical removal. |

### Table: `Jobs`

Purpose:

- Executable work item derived from a service request.

| Field | Type | Required | Key/Constraint | Meaning |
| --- | --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | `PK_Jobs` | Stable job identifier. |
| `TenantId` | `uniqueidentifier` | Yes | FK to `Tenants`, `AK_Jobs_TenantId_Id` | Tenant ownership discriminator for all job relations. |
| `ServiceRequestId` | `uniqueidentifier` | Yes | FK composite to `ServiceRequests (TenantId, Id)` | Parent request from which the job was created. |
| `AssignmentStatus` | `tinyint` | Yes | default `0`, `IX_Jobs_TenantId_AssignmentStatus` | Worker assignment lifecycle state (`AssignmentStatus` enum). |
| `AssignedWorkerUserId` | `uniqueidentifier` | No | FK composite to `Users (TenantId, Id)` | Optional currently assigned worker user. |
| `CreatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row creation timestamp. |
| `UpdatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row update timestamp. |
| `IsDeleted` | `bit` | Yes | default `0` | Soft-delete marker for logical removal. |

### Table: `Subscriptions`

Purpose:

- Tenant commercial plan and coverage windows.

| Field | Type | Required | Key/Constraint | Meaning |
| --- | --- | --- | --- | --- |
| `Id` | `uniqueidentifier` | Yes | `PK_Subscriptions` | Stable subscription identifier. |
| `TenantId` | `uniqueidentifier` | Yes | FK to `Tenants`, `AK_Subscriptions_TenantId_Id` | Owning tenant for this subscription record. |
| `PlanCode` | `nvarchar(32)` | Yes | `IX_Subscriptions_TenantId_PlanCode` | Commercial plan/tier code attached to subscription. |
| `StartsOnUtc` | `datetime2(3)` | Yes | `IX_Subscriptions_TenantId_StartsOnUtc` | Inclusive UTC start timestamp of subscription period. |
| `EndsOnUtc` | `datetime2(3)` | No | `IX_Subscriptions_TenantId_EndsOnUtc` | Optional UTC end timestamp; null indicates ongoing subscription. |
| `CreatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row creation timestamp. |
| `UpdatedAtUtc` | `datetime2(3)` | Yes | default `GETUTCDATE()` | UTC row update timestamp. |
| `IsDeleted` | `bit` | Yes | default `0` | Soft-delete marker for logical removal. |

## Enum Value Dictionary

### `ServiceRequests.Status` (`ServiceRequestStatus`)

- `0` = `New`
- `1` = `Assigned`
- `2` = `InProgress`
- `3` = `OnHold`
- `4` = `Completed`
- `5` = `Cancelled`

### `Jobs.AssignmentStatus` (`AssignmentStatus`)

- `0` = `Unassigned`
- `1` = `PendingAcceptance`
- `2` = `Accepted`
- `3` = `Rejected`
- `4` = `Completed`
- `5` = `Cancelled`

## Notes

- All Phase 1 entities include audit and soft-delete fields.
- `__EFMigrationsHistory` is excluded from the business data dictionary because it is an EF internal metadata table.
- This document complements `database/PHASE1_RELATIONAL_TABLE_DESIGN.md` and serves as the formal Phase 1.5.1 ERD/data dictionary artifact.
````
