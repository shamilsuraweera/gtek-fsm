# Phase 1.2.1 - Relational Table Design

This document defines the relational table shape for Phase 1 aggregate roots:

- `Tenant`
- `User`
- `ServiceRequest`
- `Job`
- `Subscription`

Scope of this task:

- Table design, key layout, and foreign key relationships.
- Tenant-aware relationship enforcement.
- SQL Server data types and nullability aligned with current domain invariants.

Out of scope for this task:

- EF Core fluent mappings (Phase `1.2.2`).
- Index and uniqueness tuning (Phase `1.2.3`).
- Audit and soft-delete columns (Phase `1.2.4`).

## Design Principles

- Database model: shared database, shared schema, tenant discriminator (`TenantId`).
- Aggregate primary identity uses `uniqueidentifier` (`Id`).
- Every tenant-owned table includes `TenantId uniqueidentifier not null`.
- Cross-aggregate tenant safety is enforced with composite foreign keys that include `TenantId`.
- Enum-backed states are stored as `tinyint`.

## Tables

### 1) `Tenants`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `Code` | `nvarchar(32)` | No | Tenant code (domain max 32) |
| `Name` | `nvarchar(120)` | No | Tenant name (domain max 120) |
| `ActiveSubscriptionId` | `uniqueidentifier` | Yes | Optional link to current subscription |

Constraints:

- `PK_Tenants` on (`Id`)
- `UQ_Tenants_Code` on (`Code`)

### 2) `Users`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Owner tenant |
| `ExternalIdentity` | `nvarchar(128)` | No | External identity key (domain max 128) |
| `DisplayName` | `nvarchar(120)` | No | Display name (domain max 120) |

Constraints:

- `PK_Users` on (`Id`)
- `AK_Users_TenantId_Id` unique on (`TenantId`, `Id`) for tenant-safe composite references
- `FK_Users_Tenants_TenantId` (`TenantId`) -> `Tenants`(`Id`)

### 3) `ServiceRequests`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Owner tenant |
| `CustomerUserId` | `uniqueidentifier` | No | User that created the request |
| `Title` | `nvarchar(180)` | No | Request title (domain max 180) |
| `Status` | `tinyint` | No | `ServiceRequestStatus` enum |
| `ActiveJobId` | `uniqueidentifier` | Yes | Optional currently linked job |

Constraints:

- `PK_ServiceRequests` on (`Id`)
- `AK_ServiceRequests_TenantId_Id` unique on (`TenantId`, `Id`) for tenant-safe composite references
- `FK_ServiceRequests_Tenants_TenantId` (`TenantId`) -> `Tenants`(`Id`)
- `FK_ServiceRequests_Users_TenantId_CustomerUserId` (`TenantId`, `CustomerUserId`) -> `Users`(`TenantId`, `Id`)
- `FK_ServiceRequests_Jobs_TenantId_ActiveJobId` (`TenantId`, `ActiveJobId`) -> `Jobs`(`TenantId`, `Id`)

### 4) `Jobs`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Owner tenant |
| `ServiceRequestId` | `uniqueidentifier` | No | Parent service request |
| `AssignmentStatus` | `tinyint` | No | `AssignmentStatus` enum |
| `AssignedWorkerUserId` | `uniqueidentifier` | Yes | Optional assigned worker |

Constraints:

- `PK_Jobs` on (`Id`)
- `AK_Jobs_TenantId_Id` unique on (`TenantId`, `Id`) for tenant-safe composite references
- `FK_Jobs_Tenants_TenantId` (`TenantId`) -> `Tenants`(`Id`)
- `FK_Jobs_ServiceRequests_TenantId_ServiceRequestId` (`TenantId`, `ServiceRequestId`) -> `ServiceRequests`(`TenantId`, `Id`)
- `FK_Jobs_Users_TenantId_AssignedWorkerUserId` (`TenantId`, `AssignedWorkerUserId`) -> `Users`(`TenantId`, `Id`)

### 5) `Subscriptions`

| Column | Type | Null | Notes |
|---|---|---|---|
| `Id` | `uniqueidentifier` | No | Primary key |
| `TenantId` | `uniqueidentifier` | No | Owner tenant |
| `PlanCode` | `nvarchar(32)` | No | Plan code (domain max 32) |
| `StartsOnUtc` | `datetime2` | No | Subscription start |
| `EndsOnUtc` | `datetime2` | Yes | Optional end |

Constraints:

- `PK_Subscriptions` on (`Id`)
- `AK_Subscriptions_TenantId_Id` unique on (`TenantId`, `Id`) for tenant-safe composite references
- `FK_Subscriptions_Tenants_TenantId` (`TenantId`) -> `Tenants`(`Id`)
- `FK_Tenants_Subscriptions_Id_ActiveSubscriptionId` (`Id`, `ActiveSubscriptionId`) -> `Subscriptions`(`TenantId`, `Id`)

## Tenant-Safe Relationship Model

The following relationships are tenant-enforced at the key level:

- `Users (TenantId, Id)` must match owning tenant.
- `ServiceRequests.CustomerUserId` must reference a user in the same `TenantId`.
- `Jobs.ServiceRequestId` must reference a request in the same `TenantId`.
- `Jobs.AssignedWorkerUserId` (when present) must reference a user in the same `TenantId`.
- `ServiceRequests.ActiveJobId` (when present) must reference a job in the same `TenantId`.
- `Tenants.ActiveSubscriptionId` (when present) must reference a subscription where `Subscriptions.TenantId = Tenants.Id`.

This avoids accidental cross-tenant links even if application-level checks regress.

## Enum Storage Mapping

- `ServiceRequests.Status` maps to `ServiceRequestStatus` (`tinyint`).
- `Jobs.AssignmentStatus` maps to `AssignmentStatus` (`tinyint`).

`WorkerAvailabilityStatus` is a domain lifecycle enum not yet persisted in a Phase 1 aggregate table.

## Constraint Ordering Note

Because `ServiceRequests.ActiveJobId` and `Jobs.ServiceRequestId` can create a dependency cycle,
foreign keys should be created in migration steps after both tables exist.
`Tenants.ActiveSubscriptionId` should also be added after `Subscriptions` is created.
