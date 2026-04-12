# Continuous Improvement Cadence

This document defines the recurring KPI review loop for GTEK FSM management operations.

## Objective

Turn measurable regressions from the management analytics pipeline into a prioritized operational backlog on a fixed cadence.

## Cadence

- Cadence name: Weekly KPI Review
- Frequency: Weekly
- Default review window: 7 days
- Primary owner: Operations Manager
- Supporting owners: Dispatch Lead, Service Delivery Manager, Workforce Manager, Security and Governance Lead, Platform Optimization Lead
- Review input source: `/management/reports` analytics overview

## Review Workflow

1. Refresh the management reports view for the active review window.
2. Inspect anomaly indicators and the continuous improvement section.
3. Convert every `High` priority improvement item into an explicit backlog task for the next sprint or operations cycle.
4. Convert `Medium` priority items into either a planned backlog task or a documented watchlist decision.
5. Keep `Low` priority items as monitor-only signals unless they repeat in two consecutive reviews.
6. Record the selected owner, due date, and intended outcome for each backlog item.
7. Re-check the same KPI in the next review to confirm whether the regression was reduced, cleared, or worsened.

## KPI To Backlog Rule

- A KPI regression is actionable when the management reporting pipeline emits a continuous-improvement item.
- Every emitted item must include: metric, current state, target state, recommended action, priority, and review owner.
- High-priority items are immediate backlog candidates.
- Medium-priority items require a follow-up decision during the same review cycle.
- Empty improvement output means no KPI regression currently requires backlog creation.

## Expected Outputs

- A prioritized improvement backlog linked to measurable KPI drift.
- A review record that identifies which items were accepted, deferred, or monitored.
- A repeatable weekly decision point for operational tuning, staffing, governance, and decisioning adjustments.

## CI Guardrail

The repository validates the cadence artifacts in `.github/workflows/quality-checks.yml`.
The workflow checks that:

- this document exists,
- the KPI rule file exists,
- the rule file has unique codes,
- each rule has a priority, trigger, target, action, and owner.

## Related Files

- `config/CONTINUOUS_IMPROVEMENT_KPI_RULES.json`
- `web-portal/Pages/Management/Reports.razor`
- `backend/application/Reporting/ManagementReportingQueryService.cs`
- `.github/workflows/quality-checks.yml`