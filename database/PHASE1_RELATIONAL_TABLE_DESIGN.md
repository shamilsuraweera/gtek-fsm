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

## Indexes and Uniqueness (Phase 1.2.3)

Required indexes and uniqueness constraints for high-frequency lookups and integrity:

- `Tenants`
	- `UQ_Tenants_Code` unique on (`Code`)
	- `IX_Tenants_ActiveSubscriptionId` on (`ActiveSubscriptionId`)

- `Users`
	- `UQ_Users_TenantId_ExternalIdentity` unique on (`TenantId`, `ExternalIdentity`)
	- `IX_Users_TenantId` on (`TenantId`)
	- `IX_Users_TenantId_DisplayName` on (`TenantId`, `DisplayName`)

- `ServiceRequests`
	- `IX_ServiceRequests_TenantId_Status` on (`TenantId`, `Status`)
	- `IX_ServiceRequests_TenantId_CustomerUserId` on (`TenantId`, `CustomerUserId`)
	- `UQ_ServiceRequests_TenantId_ActiveJobId` unique on (`TenantId`, `ActiveJobId`) filtered where `ActiveJobId is not null`

- `Jobs`
	- `IX_Jobs_TenantId_ServiceRequestId` on (`TenantId`, `ServiceRequestId`)
	- `IX_Jobs_TenantId_AssignmentStatus` on (`TenantId`, `AssignmentStatus`)
	- `IX_Jobs_TenantId_AssignedWorkerUserId_AssignmentStatus` on (`TenantId`, `AssignedWorkerUserId`, `AssignmentStatus`)

- `Subscriptions`
	- `IX_Subscriptions_TenantId` on (`TenantId`)
	- `IX_Subscriptions_TenantId_PlanCode` on (`TenantId`, `PlanCode`)
	- `IX_Subscriptions_TenantId_StartsOnUtc` on (`TenantId`, `StartsOnUtc`)
	- `IX_Subscriptions_TenantId_EndsOnUtc` on (`TenantId`, `EndsOnUtc`)

These are implemented in EF Core configuration classes under:

- `backend/infrastructure/Persistence/Configurations`

## Constraint Ordering Note

Because `ServiceRequests.ActiveJobId` and `Jobs.ServiceRequestId` can create a dependency cycle,
foreign keys should be created in migration steps after both tables exist.
`Tenants.ActiveSubscriptionId` should also be added after `Subscriptions` is created.

## Auditing and Soft-Delete Strategy (Phase 1.2.4)

All Phase 1 aggregate root tables include auditing columns and soft-delete capability:

### Auditing Columns

Every table includes:

| Column | Type | Details |
|---|---|---|
| `CreatedAtUtc` | `datetime2(3)` | Timestamp when record was created. Generated by SQL Server `GETUTCDATE()` on insert. Read-only after creation. |
| `UpdatedAtUtc` | `datetime2(3)` | Timestamp when record was last modified. Automatically updated by SQL Server on insert or update. Read-only to application. |
| `IsDeleted` | `bit` | Soft-delete flag; defaults to `false` (0). Set to `true` (1) instead of physically removing records. |

### Soft-Delete Strategy

- **No physical deletes**: All records are soft-deleted by setting `IsDeleted = true`.
- **Automatic query filtering**: EF Core query filters (`.HasQueryFilter()`) automatically exclude `IsDeleted = true` records from all queries.
- **Tenant boundary safety**: Soft-deleted records are per-tenant, so even if accidentally exposed, they remain within the correct tenant scope.
- **Recovery capability**: Soft-deleted records remain in the database and can be queried via `.IgnoreQueryFilters()` for audit trails or recovery scenarios.
- **Cascading soft-deletes**: When a parent aggregate is soft-deleted, child references automatically resolve to null by virtue of the exclusion filter, avoiding orphaned data visibility.

### Implementation Details

- **EF Core Configuration**:
  - Each aggregate's `Configuration` class maps auditing properties with `HasColumnType("datetime2(3)")`, `HasDefaultValueSql()`, and `ValueGeneratedOnAdd/Update()`.
  - Global query filters are applied in `GtekFsmDbContext.OnModelCreating()` via `modelBuilder.Entity<T>().HasQueryFilter(x => !x.IsDeleted)`.

- **Database Defaults**:
  - `CreatedAtUtc` and `UpdatedAtUtc` use SQL Server `GETUTCDATE()` as default; no application timestamp generation required.
  - `IsDeleted` defaults to `0` (false) for all new records.

- **Domain Layer**:
  - `CreatedAtUtc`, `UpdatedAtUtc`, and `IsDeleted` are internal-settable properties on domain aggregates (not part of public commands).
  - Soft-delete commands are not yet defined in domain logic; Phase 2+ will add soft-delete operations and audit reasoning.

### Tables and Auditing

All five Phase 1 aggregates include auditing:

- `Tenants` - CreatedAtUtc, UpdatedAtUtc, IsDeleted
- `Users` - CreatedAtUtc, UpdatedAtUtc, IsDeleted
- `ServiceRequests` - CreatedAtUtc, UpdatedAtUtc, IsDeleted
- `Jobs` - CreatedAtUtc, UpdatedAtUtc, IsDeleted
- `Subscriptions` - CreatedAtUtc, UpdatedAtUtc, IsDeleted

Auditing columns will help with compliance, troubleshooting, audit trails, and archive scenarios.
