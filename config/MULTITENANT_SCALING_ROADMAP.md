# Multi-Tenant Scaling Roadmap

**Document:** Long-term platform scaling strategy for tenant growth, isolation improvements, and operational scale
**Status:** Planning (Phase 12+)
**Last Updated:** April 13, 2026
**Owner:** Platform Architecture
**Dependencies:** Phases 0–11 maturity (audit compliance, observability baseline, multi-tenancy foundation)

---

## 1. Current State Baseline

### 1.1 Multi-Tenancy Foundation (Phase 0–11 Delivered)

The GTEK FSM platform currently operates under a **shared database, shared schema** multi-tenancy model with the following maturity:

- **Tenant Isolation Model:** TenantId GUID discriminator applied at query/service/API boundaries; all tables include soft-delete and audit fields
- **Tenant Resolution:** JWT claim first, fallback to X-Tenant-Id header, rejection if both absent
- **Tenant Propagation:** Automatic through API SecurityContext, service layers, repository queries, and SignalR connections
- **Data Protection:** Queries filtered by TenantId by default; cross-tenant writes blocked at service/repository boundaries
- **Client Context:** Mobile app operates single-tenant (no tenant switch); web portal is tenant-scoped by default

### 1.2 Observability & Compliance Foundation (Phase 11 Delivered)

- **Observability Baseline:** Correlation ID propagation (X-Correlation-Id), structured telemetry logging (method, path, HTTP status, latency), tenant-safe metrics (api_requests_total, realtime_publishes_total, authorization_decisions_total)
- **Audit Compliance:** Full audit log queryability by tenant/user/action/time-range; CSV export capability; 365-day retention policy with archive/purge strategy documented; integration tests covering all queryable dimensions
- **Authorization Audit:** Decision logging (allow/deny) with decision details, user context, resource, and outcome

### 1.3 SQL Server Multi-Tenancy Construction (Phase 0 Delivered)

- **Database Naming Convention:** `GTEK_FSM_{Environment}_Tenant_{TenantId}` (e.g., `GTEK_FSM_Prod_Tenant_550e8400-e29b-41d4-a716-446655440000`)
- **Connection Strategy:** Per-tenant database override via `SqlConnectionStringBuilder`; environment-specific configs (Development/Staging/Production); connection pooling enabled
- **Current Limitation:** Single logical connection per tenant; no cross-tenant query federation; requires tenant-specific database provisioning at onboarding

### 1.4 Operational Maturity

- **CI/CD:** GitHub Actions pipeline with quality checks, build validation, deployment gates
- **Testing:** Backend integration tests (query service, repository, API routes), portal end-to-end and security tests, domain tests
- **Documentation:** Phase completion reports for all delivered phases; architecture decisions (tenancy, SQL, observability) documented; KPI-driven continuous improvement cadence established (Phase 12.4)

---

## 2. Scaling Dimensions

### 2.1 Tenant Onboarding Automation

**Current State:** Manual onboarding process; operator provisions database, creates tenant record, issues JWT credentials.

**Future Goals:**
- Self-service tenant registration API (Phase 12.5.1)
- Automated database provisioning or database-as-a-service integration (Phase 13)
- Customizable subscription plans and tier-based feature access (Phase 14)
- Automated compliance attestation and governance readiness checks (Phase 14+)

**Business Impact:** Reduce onboarding time from hours/days to minutes; enable growth beyond manual capacity.

### 2.2 Isolation & Security Hardening

**Current State:** Shared database/schema model with TenantId discriminator; tenant-aware authorization policies; no per-tenant resource quotas.

**Future Goals:**
- Database-per-tenant isolation option for compliance-sensitive tenants (Phase 13+)
- Row-level security (RLS) policies as isolation assurance mechanism (Phase 13)
- Tenant-specific encryption keys (Phase 14)
- Multi-factor authentication and tenant-specific SSO (Phase 12.6)
- Network-level isolation for on-premises deployments (Phase 15+)

**Business Impact:** Enable regulated industries (healthcare, finance) to meet data residency/isolation requirements; support hybrid on-premises deployments.

### 2.3 Control Plane Growth

**Current State:** Monolithic administration; single authorization model; limited tenant-specific governance.

**Future Goals:**
- Tenant-specific administrative roles and policies (Phase 12.7)
- Delegated admin capabilities (tenant admins manage their own users/data without platform admin intervention) (Phase 13)
- Quota monitoring and enforcement (active jobs, concurrent workers, API rate limits) (Phase 13+)
- Billing & usage metering (Phase 14+)
- Tenant-specific audit policy configuration (Phase 14+)

**Business Impact:** Enable SaaS-style operations; reduce platform support overhead; give tenants control over their operational policies.

### 2.4 Observability & Compliance Scaling

**Current State:** Centralized observability; audit logs queryable by tenant; single retention policy for all tenants.

**Future Goals:**
- Tenant-specific observability dashboards and metrics exports (Phase 13+)
- Tenant-customizable retention policies (Phase 13+)
- Compliance report generation (SOC 2, HIPAA attestation templates) (Phase 14+)
- Advanced threat detection (anomaly detection on tenant behavior, unauthorized access patterns) (Phase 15+)

**Business Impact:** Provide enterprise tenants with full visibility into their operations; support regulatory compliance reporting; detect security incidents early.

### 2.5 Data & Platform Scaling

**Current State:** Single database instance per environment; batch job processing; limited federation.

**Future Goals:**
- Database sharding strategy for storage growth beyond single-instance limits (Phase 14+)
- Read replicas for analytics workloads without impacting operational database (Phase 13+)
- Distributed job processing (Phase 14+)
- Cross-tenant federation for platform-wide analytics (Phase 15+)

**Business Impact:** Support thousands of tenants without operational scaling limits; enable rich analytics across platform growth.

---

## 3. Milestone Roadmap

### Phase 12 (Current): Foundation & Early Automation

#### 12.5.1 — Self-Service Tenant Onboarding API
- **Objective:** Enable software to request tenant provisioning without manual intervention
- **Success Criteria:**
  - REST API endpoint accepts tenant registration request (name, plan tier, admin email)
  - Tenant record created; database provisioned (initial schema applied)
  - JWT credentials issued and sent to admin email
  - Audit log captures onboarding event
  - End-to-end integration tests validate registration flow
- **Prerequisites:** Phases 0–11 complete
- **Dependencies:** None (can run in parallel with other Phase 12 work)
- **Estimated Scope:** Backend API, integration tests, documentation
- **Readiness Gates:**
  - Entry: Phase 11 complete, tenant isolation audit passed, security review approved
  - Exit: Onboarding API tested end-to-end, audit logging covers all tenant CRUD, compliance impact doc updated

#### 12.6 — Multi-Factor Authentication & Tenant-Specific SSO
- **Objective:** Enable tenant-specific identity providers; strengthen authentication posture
- **Success Criteria:**
  - MFA setup UI in management portal
  - SSO configuration API (OAuth2/OIDC endpoints configurable per-tenant)
  - Token refresh logic maintains tenant context through OAuth flow
  - Integration tests validate MFA & SSO flows
- **Prerequisites:** 12.5.1 (self-service onboarding available), Phase 11 audit baseline
- **Dependencies:** None (can run in parallel)
- **Estimated Scope:** Backend identity service extensions, portal MFA/SSO settings component, integration & E2E tests
- **Readiness Gates:**
  - Entry: Security architecture review approved, MFA library selection finalized
  - Exit: SSO integration tested with mock IdP, MFA enrollment/recovery tested, security audit passed

#### 12.7 — Tenant-Specific Administrative Roles
- **Objective:** Enable tenants to define and enforce role-based access control within their account
- **Success Criteria:**
  - Tenant admin can define custom roles and assign permissions to users
  - Authorization checks at API request boundary enforce tenant-specific roles
  - Audit log records role changes and permission decisions
  - Portal UI allows role and permission management
- **Prerequisites:** 12.5.1 (onboarding foundation), Phase 11 authorization decision audit
- **Dependencies:** None
- **Estimated Scope:** Backend authorization service, role/permission schema, portal admin interface, compliance tests
- **Readiness Gates:**
  - Entry: Authorization architecture review
  - Exit: Custom roles created and enforced, audit events confirmed, security test suite passes

---

### Phase 13+: Mid-Term Scaling & Enterprise Hardening

#### 13.1 — Database-Per-Tenant Isolation Option
- **Objective:** Support database-per-tenant deployment model for compliance-sensitive workloads
- **Success Criteria:**
  - Onboarding API accepts isolation strategy selector (shared vs. dedicated)
  - Provisioning logic detects strategy and creates tenant database accordingly
  - Multi-database connection pool strategy implemented and tested
  - Query service abstracts underlying database selection
  - Schema migration tooling validates consistency across both models
- **Prerequisites:** 12.5.1 onboarding API, Phase 10 schema migration framework, production database strategy finalized
- **Dependencies:** Requires review and approval from security & compliance team
- **Estimated Scope:** Provisioning service rewrites, connection pool strategy, schema consistency validator, E2E tests
- **Readiness Gates:**
  - Entry: Security architecture review for per-tenant DB, capacity & cost modeling approved
  - Exit: Both shared & dedicated isolation options successfully deployed to staging, schema consistency verified, production runbook documented

#### 13.2 — Row-Level Security (RLS) Policies
- **Objective:** Introduce SQL Server RLS as an additional isolation enforcement layer
- **Success Criteria:**
  - RLS policies created for all tables filtering by TenantId
  - SQL Server predicate logic enforced alongside application-layer filtering
  - Performance impact assessed and optimized (indexed predicates, execution plan review)
  - Integration tests validate RLS blocks cross-tenant queries
- **Prerequisites:** Database-per-tenant option available (Phase 13.1), RLS pilot complete
- **Dependencies:** None
- **Estimated Scope:** Schema updates, RLS policy deployment, performance tests, operational documentation
- **Readiness Gates:**
  - Entry: RLS performance baseline established, DBA review approved
  - Exit: RLS policies in place, query performance validated, production deploy guide documented

#### 13.3 — Tenant-Specific Observability Dashboards
- **Objective:** Provide each tenant with visibility into their own operational metrics
- **Success Criteria:**
  - Metrics aggregated and filtered by TenantId
  - Portal dashboard component renders tenant-scoped metrics
  - Metrics export API available (CSV/JSON)
  - Metric storage strategy supports multi-year retention per tenant
- **Prerequisites:** Phase 11.6 observability baseline, 12.5.1 onboarding
- **Dependencies:** None
- **Estimated Scope:** Portal dashboard component, metrics query service updates, export API, tests
- **Readiness Gates:**
  - Entry: Metrics retention & cost model approved, dashboard design wireframes validated
  - Exit: Metrics dashboard rendered correctly per-tenant, export validated, performance benchmarked

#### 13.4 — Quota Monitoring & Enforcement
- **Objective:** Enforce tenant-specific limits on active jobs, concurrent workers, API requests
- **Success Criteria:**
  - Tenant quota configuration stored and enforced
  - API request rate limiter blocks requests beyond quota
  - Active job counter prevents exceeding job limit
  - Portal quota usage widget shows consumption vs. limit
  - Quota threshold alerts notify tenant admin
- **Prerequisites:** 12.5.1 onboarding (plan tiers defined), Phase 11.6 observability baseline
- **Dependencies:** Alert/notification service (Phase 11+ assumption)
- **Estimated Scope:** Quota service, rate limiter middleware, portal quota widget, integration tests
- **Readiness Gates:**
  - Entry: Quota model designed, tier definitions approved
  - Exit: Quotas enforced in staging, alerts triggered correctly, SLA guarantees documented

---

### Phase 14+: Enterprise & Scaling Maturity

#### 14.1 — Billing & Usage Metering
- **Objective:** Enable consumption-based billing; meter usage per tenant
- **Success Criteria:**
  - Meter records created for all billable events (API calls, job executions, data storage)
  - Metrics aggregated to billing dimension (monthly usage by tenant)
  - Billing reports generated and exportable
  - Integration with payment gateway (Stripe/PayPal)
- **Prerequisites:** 12.5.1 onboarding, 13.4 quota monitoring
- **Dependencies:** Billing backend service, payment gateway accounts
- **Estimated Scope:** Metering service, billing aggregation, payment integration, portal billing UI
- **Readiness Gates:**
  - Entry: Billing model defined, pricing tiers approved, payment vendor selected
  - Exit: Metering tested end-to-end, billing reports accurate, payment flow tested with sandbox

#### 14.2 — Compliance Report Generation
- **Objective:** Enable automated compliance attestation (SOC 2, HIPAA, GDPR)
- **Success Criteria:**
  - Report templates for common compliance frameworks
  - Audit log queries generate compliance evidence
  - Reports exportable as PDF/JSON
  - Report schedule configurability (generate on demand or periodic)
- **Prerequisites:** Phase 11.5 audit compliance baseline, 12.5.1 onboarding, tenant-specific audit policies (future)
- **Dependencies:** None
- **Estimated Scope:** Report generator service, template library, portal report UI, tests
- **Readiness Gates:**
  - Entry: Compliance framework review, report template validation by legal/compliance
  - Exit: Sample compliance report generated, audit data completeness verified, template accuracy validated

#### 14.3 — Distributed Job Processing
- **Objective:** Scale job execution beyond single-node limits; enable job federation
- **Success Criteria:**
  - Job queue distributes to multiple worker processes/nodes
  - Job state machine tracks job lifecycle across distributed nodes
  - Failure recovery and retry logic implemented
  - Portal job monitor shows real-time distributed job status
- **Prerequisites:** Phase 7 job execution baseline, 13.4 quota monitoring
- **Dependencies:** Message queue (RabbitMQ/Azure Service Bus), job state persistence
- **Estimated Scope:** Job distribution service, queue consumer, state machine updates, monitoring, tests
- **Readiness Gates:**
  - Entry: Message queue architecture approved, job failure scenarios mapped
  - Exit: Distributed job execution tested under load, failover scenarios validated, operations runbook documented

#### 14.4 — Database Sharding Strategy
- **Objective:** Prepare for storage and query performance scaling beyond single-instance limits
- **Success Criteria:**
  - Sharding strategy documented (tenant ID hash, geographic, time-based)
  - Shard key propagation through service layer
  - Shard selection logic implemented and tested
  - Query federation for cross-shard aggregations
  - Schema migration tooling supports sharded environments
- **Prerequisites:** 13.1 database-per-tenant option, Phase 0 SQL strategy, performance baseline established
- **Dependencies:** None (documentation phase; implementation in Phase 15)
- **Estimated Scope:** Architecture document, shard key design, prototype implementation, tests
- **Readiness Gates:**
  - Entry: Capacity model predicting shard necessity, data distribution analysis complete
  - Exit: Sharding architecture approved, prototype tested, operational handoff guide documented

---

### Phase 15+ (Future): Platform Federation & Advanced Operations

#### 15.1 — Cross-Tenant Federation & Platform Analytics
- **Objective:** Enable platform-wide insights without violating tenant isolation
- **Success Criteria:**
  - Aggregation queries compute platform-wide metrics without accessing individual tenant data
  - Anonymous dashboards show industry benchmarks and trend analysis
  - Tenant consent model controls contribution to aggregate metrics
  - Federation queries tested for security (no data leakage)
- **Prerequisites:** 14.1 billing/metering, 14.4 sharding strategy, Phase 14+ isolation maturity
- **Dependencies:** None
- **Estimated Scope:** Federation query service, consent model, anonymous metric aggregation, tests
- **Readiness Gates:**
  - Entry: Privacy policy updated to cover federation, data sharing consent model designed
  - Exit: Federation queries return correct aggregates, privacy audit passed, tenant consent recorded

#### 15.2 — Advanced Threat Detection
- **Objective:** Detect security anomalies within tenant behavior; early incident warning
- **Success Criteria:**
  - Machine learning model trained on tenant behavior baseline
  - Real-time anomaly detector flags unusual patterns (burst API calls, failed auth attempts, data export spike)
  - Alert routing to tenant security contact
  - Portal incident history records anomalies and resolutions
- **Prerequisites:** 13.3 observability dashboards, Phase 11 audit compliance baseline, 14.2 compliance reports
- **Dependencies:** ML platform/libraries, alerting service
- **Estimated Scope:** ML model training pipeline, anomaly detector service, alert routing, portal incident UI
- **Readiness Gates:**
  - Entry: Threat model defined, ML model validation against known threats
  - Exit: Anomaly detector tested with synthetic attack patterns, false positive rate acceptable, playbook for response documented

#### 15.3 — On-Premises & Hybrid Cloud Deployment
- **Objective:** Support regulated customers requiring on-premises deployment or hybrid cloud setup
- **Success Criteria:**
  - Deployment model selector (Cloud SaaS vs. On-Premises vs. Hybrid)
  - Network isolation & firewall rules documented for on-premises
  - License key system controls feature access per deployment model
  - Upgraded schema migration tooling validates consistency across deployments
- **Prerequisites:** 13.1 database-per-tenant, 13.2 RLS policies, Phase 0 deployment strategy
- **Dependencies:** Licensing backend service, network architecture review
- **Estimated Scope:** Deployment configuration service, license key validation, network deployment guide, architecture diagram
- **Readiness Gates:**
  - Entry: On-premises security requirements gathered, network architecture approved, licensing model finalized
  - Exit: On-premises deployment piloted with trusted customer, network isolation verified, operational runbook completed

---

## 4. Risk & Assumptions

### 4.1 Technical Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Database sharding complexity increases operational overhead | High | Implement phased sharding; begin with tenant-based sharding on read replicas before write sharding |
| Multi-database connection strategies reduce connection pool efficiency | Medium | Implement connection pool federation; benchmark connection acquisition latency before production |
| RLS policy performance degrades with complex tenant hierarchies | Medium | Profile RLS execution plans; optimize indexed predicates; use caching for complex policies |
| Distributed job processing introduces failure modes (network, queue poison) | High | Implement dead-letter queue handling; automated retry with backoff; operational playbooks for common failure scenarios |

### 4.2 Compliance Risks

| Risk | Impact | Mitigation |
|------|--------|-----------|
| Cross-tenant data federation violates GDPR/CCPA requirements | High | Implement explicit tenant consent model; anonymize aggregates; legal review before federation queries run |
| On-premises deployments introduce audit/compliance divergence | Medium | Unified audit contract across deployments; deployment-specific compliance checklists; cross-deployment audit validation tests |
| Database-per-tenant adds infrastructure complexity; audit coverage becomes fragmented | Medium | Centralized audit log aggregation; unified query federation across tenant databases |

### 4.3 Operational Assumptions

- **Assumption:** Tenant API call volume grows predictably; quota enforcement becomes the primary scaling lever before database sharding is required (Phase 14+)
- **Assumption:** Phase 11 observability baseline provides sufficient infrastructure to detect and alert on scaling problems
- **Assumption:** Self-service onboarding (Phase 12.5.1) reduces manual provisioning overhead by >80% within 6 months of production deployment
- **Assumption:** Tenant-specific administrative roles (Phase 12.7) reduce platform support burden by >50% within 12 months

---

## 5. Integration with Existing Architecture

### 5.1 Cross-References to Existing Documentation

This roadmap builds directly on the following delivered artifacts:

1. **`config/tenancy-approach.json`** — Defines current-state tenant model (shared database/schema, TenantId discriminator). Roadmap Phase 13.1+ explores database-per-tenant evolution while maintaining backward compatibility.

2. **`config/SQL_SERVER_STRATEGY.md`** — Documents multi-tenancy via database naming convention (`GTEK_FSM_{Env}_Tenant_{TenantId}`) and per-tenant connection override. Roadmap Phase 13.1+ layers on advanced connection pooling and sharding strategies.

3. **`config/PHASE11_OBSERVABILITY_BASELINE.md`** — Establishes correlation IDs, structured telemetry logging, metrics. Roadmap Phase 13.3+ extends to tenant-specific observability dashboards and metric exports.

4. **`config/PHASE11_AUDIT_COMPLIANCE_READINESS_REPORT.md`** — Proves audit queryability by tenant/user/action/time-range and 365-day retention policy. Roadmap Phase 14.2+ builds compliance report generation on this audit foundation.

5. **`config/CONTINUOUS_IMPROVEMENT_CADENCE.md`** — Establishes KPI monitoring and weekly review cadence (Phase 12.4). Roadmap milestones integrate into continuous improvement loop; scaling KPIs (onboarding time, quota violations, cross-tenant federation accuracy) tracked weekly.

### 5.2 Dependency Chain

```
Phase 12 (Current)
  ├── 12.4: KPI-Driven Continuous Improvement ✓ DONE
  ├── 12.5: Multi-Tenant Scaling Roadmap (this document) ← CURRENT
  ├── 12.6: MFA & Tenant-Specific SSO
  └── 12.7: Tenant-Specific Administrative Roles
    ↓
Phase 13 (Mid-Term)
  ├── 13.1: Database-Per-Tenant Isolation Option
  ├── 13.2: Row-Level Security (RLS) Policies
  ├── 13.3: Tenant-Specific Observability Dashboards
  └── 13.4: Quota Monitoring & Enforcement
    ↓
Phase 14 (Enterprise)
  ├── 14.1: Billing & Usage Metering
  ├── 14.2: Compliance Report Generation
  ├── 14.3: Distributed Job Processing
  └── 14.4: Database Sharding Strategy
    ↓
Phase 15+ (Platform Federation & Advanced Ops)
  ├── 15.1: Cross-Tenant Federation
  ├── 15.2: Advanced Threat Detection
  └── 15.3: On-Premises & Hybrid Cloud Deployment
```

### 5.3 Interface to Continuous Improvement

Weekly KPI cadence (Phase 12.4) monitors:
- **Onboarding Success Rate:** % of self-service registrations completing successfully (target >95%)
- **Quota Violation Rate:** % of tenants hitting quota limits (target <5% per week)
- **Isolation Audit Pass Rate:** % of security isolation audits passing (target 100%)
- **Compliance Report Accuracy:** % of automated compliance reports matching manual audits (Phase 14.2 target >99%)

Regressions in these KPIs trigger improvement items in the weekly cadence, feeding back into priority sequencing of roadmap milestones.

---

## 6. Roadmap Governance

### 6.1 Review & Approval Process

- **Quarterly Architecture Review:** Roadmap section reviewed for currency; milestones re-prioritized based on tenant feedback and platform KPIs
- **Phase Kickoff (6 weeks before):** Detailed execution plan created for upcoming phase; dependencies validated; readiness gates confirmed
- **Monthly Progress Check-In:** Completed milestones marked; blockers identified; forward plan adjusted if needed

### 6.2 Sequencing Constraints

1. **Phase 12.5.1 (Self-Service Onboarding) must complete before:**
   - 12.6 (SSO can integrate with onboarding flow)
   - 12.7 (Role definitions tied to onboarding context)
   - 13.4 (Quota monitoring requires tenant provisioning)

2. **Phase 11 observability/audit maturity is prerequisite for all Phase 13+ milestones**

3. **Security & compliance sign-off required before entry to Phase 13.1 (database-per-tenant) and Phase 15.3 (on-premises deployment)**

### 6.3 Success Metrics (Phase-Level KPIs)

| Metric | Target | Owner |
|--------|--------|-------|
| Tenant onboarding time (Phase 12.5.1+) | <15 minutes from API call to provisioned | Platform |
| Self-service onboarding adoption (Phase 12.5.1+) | >80% adoption within 6 months of launch | Product |
| Time-to-market for new tenant (Phase 13.1+) | <1 minute for shared DB, <5 minutes for dedicated | Platform |
| Audit compliance report accuracy (Phase 14.2) | >99% match vs. manual audit | Compliance |
| Anomaly detection false positive rate (Phase 15.2) | <5% false positives per week | Security |

---

## 7. Next Steps

1. **Review & Approve:** Architect, Security, Compliance, Product review roadmap; provide feedback on milestone sequencing, risk mitigation
2. **Detailed Planning:** For Phase 12.5.1 (self-service onboarding), create acceptance criteria breakdown and sprint-level design doc
3. **Track Progress:** Link tracker.txt milestones to roadmap sections; update roadmap quarterly
4. **Communicate:** Share roadmap with tenant advisory board; gather early adoption feedback on planned features

---

**Document History**
- 2026-04-13: Initial roadmap draft (Phase 12 baseline + Phase 13–15 planning)
