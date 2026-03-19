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

## Initial Migration Baseline (Phase 1.3.1)

The first production-shaped EF Core migration has been generated and validated.

Migration artifacts:

- `backend/infrastructure/Persistence/Migrations/20260318161457_Phase1InitialSchema.cs`
- `backend/infrastructure/Persistence/Migrations/20260318161457_Phase1InitialSchema.Designer.cs`
- `backend/infrastructure/Persistence/Migrations/GtekFsmDbContextModelSnapshot.cs`

Validation summary:

- Applied from clean database state to `GTEK_FSM_Phase1_Validation`.
- Migration applied successfully (`Applying migration '20260318161457_Phase1InitialSchema'. Done.`).
- Verified resulting base tables: `Tenants`, `Users`, `ServiceRequests`, `Jobs`, `Subscriptions` plus `__EFMigrationsHistory`.

## Baseline Reference Seed Data (Phase 1.3.2)

Baseline reference seed data is defined in:

- `database/seeds/001_baseline_reference_data.sql`

Seed scope for this phase:

- Role placeholders: `Guest`, `Customer`, `Worker`, `Support`, `Manager`, `Admin`
- Tier placeholders: `FREE`, `PROFESSIONAL`, `ENTERPRISE`
- Status placeholders:
  - Request stages mapped to `ServiceRequests.Status` (`New`, `Assigned`, `InProgress`, `OnHold`, `Completed`, `Cancelled`)
  - Assignment states mapped to `Jobs.AssignmentStatus` (`Unassigned`, `PendingAcceptance`, `Accepted`, `Rejected`, `Completed`, `Cancelled`)

Implementation characteristics:

- Deterministic GUIDs for stable reference rows.
- Dedicated reference tenant (`REF-BASELINE`) for isolation.
- Guarded inserts (`IF NOT EXISTS`) to prevent duplicate reference rows when rerun.

## Seed Execution Idempotency (Phase 1.3.3)

Seed pipeline execution is idempotent across repeated local startup runs.

Runner implementation:

- `database/scripts/dev-db-seed.sh` now executes ordered SQL scripts via `sqlcmd`.
- Applied scripts are tracked in `dbo.__SeedHistory` by file name.
- If a script was previously applied, it is skipped instead of re-executed.

Safety model:

- Script-level idempotency from `dbo.__SeedHistory` prevents repeated file execution.
- Row-level idempotency remains enforced in seed SQL with `IF NOT EXISTS` patterns.

## Database Reset, Reapply, and Verification (Phase 1.3.4)

Comprehensive scripts and tasks automate the entire local database workflow.

Scripts:

- `database/scripts/dev-db-init.sh`
  - Applies pending migrations using `dotnet ef database update`.
  - Uses Infrastructure project as both `--project` and `--startup-project` for reliable EF tooling.

- `database/scripts/dev-db-reset.sh`
  - Drops the local database and reapplies all migrations from scratch.
  - Ensures a clean schema baseline for validation.
  - Uses Infrastructure project for startup to avoid EF tooling dependency issues.

- `database/scripts/dev-db-seed.sh`
  - Executes seed SQL scripts in numeric order with idempotent history tracking.
  - Skips already-applied scripts on repeated runs.
  - Loads connection settings from `.env` or environment variables.

- `database/scripts/dev-db-verify.sh`
  - Validates schema: confirms all Phase 1 tables exist.
  - Validates seed data: checks that baseline reference counts match expected values.
  - Returns success (exit 0) if all checks pass, failure (exit 1) otherwise.
  - Gracefully skips if `sqlcmd` is unavailable.

- `database/scripts/dev-db-refresh.sh`
  - Orchestrates a complete end-to-end workflow:
    1. Reset database (drop + recreate)
    2. Verify schema was created
    3. Apply seed data
    4. Verify seed data populations
  - Suitable for establishing a known-good state before development sessions.

VS Code Tasks:

- `Database: Init (Apply Migrations)` → runs dev-db-init.sh
- `Database: Reset (Drop + Recreate)` → runs dev-db-reset.sh
- `Database: Seed` → runs dev-db-seed.sh
- `Database: Refresh (Reset + Init + Seed + Verify)` → runs dev-db-refresh.sh
- `Database: Verify Schema & Data` → runs dev-db-verify.sh

Verification coverage:

- Schema validation checks:
  - `Tenants`, `Users`, `ServiceRequests`, `Jobs`, `Subscriptions`
  - `__EFMigrationsHistory` (EF migration tracking table)
- Seed data validation checks:
  - Reference tenant count: 1 (REF-BASELINE)
  - User count: 6 (role placeholders)
  - Subscription tier count: 3 (FREE, PROFESSIONAL, ENTERPRISE)
  - ServiceRequest status count: 6 (stage placeholders)
  - Job assignment status count: 6 (status placeholders)

## Migration and Seed Validation Across Local and Docker Workflows (Phase 1.3.5)

Migration and seed behavior has been validated in both required execution modes.

Validation mode A: Direct local SQL Server endpoint (non-compose)

- SQL Server started as a standalone local container mapped to `localhost:12433`.
- Database refresh workflow executed:
  - `./database/scripts/dev-db-refresh.sh`
  - Confirmed migration apply from clean state.
  - Confirmed schema tables exist (`Tenants`, `Users`, `ServiceRequests`, `Jobs`, `Subscriptions`, `__EFMigrationsHistory`).
  - Confirmed baseline seed counts after refresh (1 tenant, 6 users, 3 subscriptions, 6 service requests, 6 jobs).
- Seed idempotency re-validation executed:
  - `./database/scripts/dev-db-seed.sh`
  - `./database/scripts/dev-db-verify.sh`
  - Confirmed rerun behavior skips already applied seed file and preserves expected counts.

Validation mode B: Docker Compose SQL Server workflow

- SQL Server started through compose service:
  - `docker compose up -d sqlserver`
- Database refresh and verification executed using the same scripts:
  - `./database/scripts/dev-db-refresh.sh`
  - `./database/scripts/dev-db-seed.sh`
  - `./database/scripts/dev-db-verify.sh`
- Confirmed the same successful outcomes as direct-local flow:
  - Migration from clean state succeeded.
  - Seed execution succeeded.
  - Seed rerun remained idempotent (`Skipping (already applied): 001_baseline_reference_data.sql`).
  - Final verification passed with expected baseline counts.

Outcome:

- Phase 1.3 migration and seed pipeline is verified for both direct local and Docker-based development workflows.

## Repository Interface Contracts (Phase 1.4.1)

Application-layer repository interfaces are now defined to standardize aggregate persistence and retrieval patterns before Infrastructure implementations.

Contract location:

- `backend/application/Persistence/Repositories/`

Defined interfaces:

- `IRepository<TAggregate>`
  - Base write contract shared by aggregate repositories.
  - Methods: `AddAsync`, `Update`, `Remove`.
- `ITenantRepository`
  - Tenant retrieval by id and code plus uniqueness probe (`ExistsByCodeAsync`).
- `IUserRepository`
  - Tenant-scoped user retrieval by id and external identity plus `ListByTenantAsync`.
- `IServiceRequestRepository`
  - Tenant-scoped request retrieval by id plus list-by-tenant and list-by-customer queries.
- `IJobRepository`
  - Tenant-scoped job retrieval by id plus list-by-request and list-by-worker queries.
- `ISubscriptionRepository`
  - Tenant-scoped subscription retrieval by id plus active-subscription lookup and list-by-tenant query.

Design intent:

- Contracts are defined in Application to preserve clean architecture boundaries.
- Aggregate retrieval methods are tenant-aware where applicable to support safe multi-tenant query paths.
- Query contracts are intentionally minimal and use async/cancellation primitives to align with future EF Core implementations in Phase 1.4.2.

