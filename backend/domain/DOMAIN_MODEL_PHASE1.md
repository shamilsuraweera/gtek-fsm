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

## Value Objects (Phase 1.1.2)

The following immutable value objects are defined in `backend/domain/ValueObjects`:

- `IdentityValue`
  - Represents external identity provider + subject pair (`provider:subject`).
  - Used to normalize identity references without embedding auth provider logic in aggregates.

- `ContactDetails`
  - Encapsulates email and phone contact values with basic format validation.

- `Address`
  - Encapsulates canonical service/location address fields with ISO alpha-2 country code.

- `Money`
  - Encapsulates decimal amount + ISO-4217 currency code.
  - Enforces non-negative amounts and same-currency arithmetic.

- `Rate` and `RateUnit`
  - Represents billable rate and billing unit (`PerJob`, `PerHour`, `PerDay`).

- `SchedulingWindow`
  - Encapsulates UTC start/end timestamps and overlap logic for scheduling checks.

## Lifecycle Enums and State Transitions (Phase 1.1.3)

Lifecycle enums are defined in `backend/domain/Enums`:

- `ServiceRequestStatus`
  - `New`, `Assigned`, `InProgress`, `OnHold`, `Completed`, `Cancelled`
- `AssignmentStatus`
  - `Unassigned`, `PendingAcceptance`, `Accepted`, `Rejected`, `Completed`, `Cancelled`
- `WorkerAvailabilityStatus`
  - `Offline`, `Available`, `Busy`, `OnBreak`, `Unavailable`

Transition policies are defined in `backend/domain/Policies`:

- `ServiceRequestStateTransitions`
  - Enforces allowed request lifecycle moves.
- `AssignmentStateTransitions`
  - Enforces allowed assignment lifecycle moves.
- `WorkerAvailabilityTransitions`
  - Enforces allowed worker availability moves.

Aggregate integration:

- `ServiceRequest`
  - Starts as `New`.
  - Uses `TransitionTo(ServiceRequestStatus)` with policy validation.
- `Job`
  - Starts as `Unassigned`.
  - Uses guarded transitions through assignment methods (`AssignWorker`, `MarkAccepted`, `MarkRejected`, `MarkCompleted`, `MarkCancelled`).

## Domain Rules and Invariants (Phase 1.1.4)

Explicit guard clauses are enforced directly in aggregate behavior methods (no infrastructure dependencies):

- Shared guard helpers:
  - `backend/domain/Rules/DomainGuards.cs`
  - Centralizes required id/text and max-length validations.

- `Tenant` invariants:
  - `Code` and `Name` are required and length-constrained.
  - Active subscription id must be non-empty when attached.

- `User` invariants:
  - `TenantId` is mandatory and immutable after creation.
  - `ExternalIdentity` and `DisplayName` are required and length-constrained.

- `ServiceRequest` invariants:
  - `Title` is required/length-constrained and cannot be renamed in terminal states.
  - Job linking is only allowed in `Assigned` state and only when no active job exists.
  - Request cannot transition to `Completed` without an active linked job.
  - Job cannot be unlinked while request is `InProgress`.

- `Job` invariants:
  - Worker assignment requires non-empty worker id and no pre-existing assignment.
  - Assignment status transitions requiring worker context enforce assigned-worker presence.
  - Unassign is blocked when already unassigned, accepted, or completed.

- `Subscription` invariants:
  - Plan code is required and length-constrained.
  - End date must be after start date.
  - End can only happen once.
  - Plan cannot change after subscription has ended.

## Minimal Domain Event Contract (Phase 1.1.5)

Domain event primitives are defined in `backend/domain/Events`:

- `IDomainEvent`
  - Minimal contract: `OccurredOnUtc`, `EventName`.
- `DomainEvent`
  - Base record for immutable event payloads.

Initial event types:

- `ServiceRequestStatusChangedDomainEvent`
- `JobAssignmentStatusChangedDomainEvent`
- `TenantSubscriptionChangedDomainEvent`
- `SubscriptionPlanChangedDomainEvent`

Aggregate capture pattern:

- Each aggregate maintains in-memory `DomainEvents` collection.
- Key state-changing methods append domain events.
- `ClearDomainEvents()` exists for application/infrastructure dispatch pipelines.

Scope boundary:

- Events are capture-only in Phase 1.
- No transport, broker, SignalR, or async delivery is enabled here.

## Relational Table Design (Phase 1.2.1)

Relational table design for Phase 1 aggregate roots is defined in:

- `database/PHASE1_RELATIONAL_TABLE_DESIGN.md`

That artifact specifies table columns, SQL types, nullability, and explicit tenant-safe foreign key relationships.

## EF Core Entity Mapping (Phase 1.2.2)

Infrastructure mappings are defined in:

- `backend/infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/JobConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`

DbContext registration and model wiring:

- `backend/infrastructure/Persistence/GtekFsmDbContext.cs`

Mapping coverage implemented in this phase:

- SQL types, max lengths, required vs optional fields.
- Enum persistence for lifecycle statuses as `tinyint`.
- Date/time precision for subscription dates.
- Default values for request and assignment status columns.
- Tenant-safe composite key references for cross-aggregate foreign keys.

## Indexes and Uniqueness (Phase 1.2.3)

Tenant-scoped indexes and uniqueness constraints are configured in:

- `backend/infrastructure/Persistence/Configurations/TenantConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/UserConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/JobConfiguration.cs`
- `backend/infrastructure/Persistence/Configurations/SubscriptionConfiguration.cs`

Design-level rationale and full index list are documented in:

- `database/PHASE1_RELATIONAL_TABLE_DESIGN.md`

## Auditing Columns and Soft-Delete Strategy (Phase 1.2.4)

All Phase 1 aggregate roots include auditing and soft-delete capability:

### Domain Model Changes

All five aggregates (`Tenant`, `User`, `ServiceRequest`, `Job`, `Subscription`) now include three internal-settable properties:

- `CreatedAtUtc` (`DateTime`): Timestamp when record was first persisted.
- `UpdatedAtUtc` (`DateTime`): Timestamp of the most recent modification.
- `IsDeleted` (`bool`): Soft-delete flag; `false` by default, set to `true` instead of physically removing records.

### EF Core Configuration

Auditing mappings are added to all five configuration classes:

- `CreatedAtUtc` and `UpdatedAtUtc` are mapped to `datetime2(3)` with SQL Server `GETUTCDATE()` defaults.
- `ValueGeneratedOnAdd()` ensures `CreatedAtUtc` is set exactly once at insertion.
- `ValueGeneratedOnAddOrUpdate()` ensures `UpdatedAtUtc` is refreshed on each modification.
- `IsDeleted` is mapped to `bit` with default `false` (0).

### Global Query Filters

EF Core query filters (`.HasQueryFilter()`) are applied in `GtekFsmDbContext.OnModelCreating()` to automatically exclude soft-deleted records:

```csharp
modelBuilder.Entity<Tenant>().HasQueryFilter(x => !x.IsDeleted);
modelBuilder.Entity<User>().HasQueryFilter(x => !x.IsDeleted);
modelBuilder.Entity<ServiceRequest>().HasQueryFilter(x => !x.IsDeleted);
modelBuilder.Entity<Job>().HasQueryFilter(x => !x.IsDeleted);
modelBuilder.Entity<Subscription>().HasQueryFilter(x => !x.IsDeleted);
```

This ensures all queries exclude soft-deleted records by default while still retaining recovery and audit trail capability.

### Soft-Delete Behavior

- **No physical deletes**: Records are marked deleted, not removed from the database.
- **Query transparency**: Application code queries behave as if soft-deleted records do not exist.
- **Audit trail**: Soft-deleted records remain in the database for compliance and recovery.
- **Tenant isolation**: Soft-deleted records remain within their tenant scope even if visible via `.IgnoreQueryFilters()`.

### Full Documentation

Detailed rationale, schema design, and implementation notes are captured in:

- `database/PHASE1_RELATIONAL_TABLE_DESIGN.md` - "Auditing and Soft-Delete Strategy (Phase 1.2.4)" section

## Naming Conventions Validation (Phase 1.2.5)

Naming conventions were validated against:

- `config/naming-conventions.json`

Validated scopes:

- Table names
- Column names
- Key names (`PK_`, `AK_`)
- Foreign key names (`FK_`)
- Index and uniqueness names (`IX_`, `UQ_`)
- Migration identifier naming standard for upcoming EF migrations

Outcome:

- Current Phase 1.2 schema and EF naming are consistent.
- No schema object renames were required.
- Migration naming standard is documented in:
  - `database/PHASE1_RELATIONAL_TABLE_DESIGN.md` under "Naming Convention Validation (Phase 1.2.5)".

## Notes

- This is the minimal phase-1 aggregate shape for schema and persistence work.
- Event publishing/integration is deferred to later phases.

