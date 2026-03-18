GTEK FSM - Seed Data Strategy (Phase 0.7.3)

Purpose
- Define how local and shared development seed data is authored and applied.
- Keep seeds deterministic, idempotent, and environment-safe.

Seed File Location
- database/seeds/

Naming Convention
- Numeric prefix for execution order:
  - 001_<seed_scope>.sql
  - 002_<seed_scope>.sql
- Example:
  - 001_baseline_reference_data.sql
  - 002_demo_tenant_data.sql

Rules
- Make scripts idempotent whenever possible.
- Do not assume hard-coded identity values unless explicitly reserved.
- Keep business logic out of seed scripts.
- Keep local demo seed data separate from production-safe reference data.
- Use transactions for multi-statement seed batches.

Environment Policy
- Development: can include demo/test data.
- Staging: should include only required reference/config data.
- Production: controlled reference data only; no demo data.

Execution Flow
- Run migrations first:
  - ./database/scripts/dev-db-init.sh
- Prepare/apply seeds:
  - ./database/scripts/dev-db-seed.sh

Phase 1.3.2 Baseline Seed Activation
- `001_baseline_reference_data.sql` is now active for Phase 1 schema.
- Seed categories covered:
  - Role placeholders: `Guest`, `Customer`, `Worker`, `Support`, `Manager`, `Admin`
  - Tier placeholders: `FREE`, `PROFESSIONAL`, `ENTERPRISE`
  - Status placeholders mapped to enum-backed columns in `ServiceRequests` and `Jobs`
- Reference rows are isolated under tenant code `REF-BASELINE`.
- Deterministic GUIDs are used for stable reference identity.

Future (0.7.3+)
- Activate actual SQL execution hook in dev-db-seed.sh once schema exists.
- Add seed manifest or version tracking table if needed.
