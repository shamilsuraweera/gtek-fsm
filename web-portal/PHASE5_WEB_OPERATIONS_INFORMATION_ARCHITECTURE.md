# Phase 5.1.1 - Web Operations Information Architecture

## Purpose

Define the information architecture (IA) for the Web Operations Shell so Customer Care and Management users can quickly locate, interpret, and act on operational data with minimal navigation overhead.

## IA Principles

1. High-visibility first: queue state, bottlenecks, and SLA risk indicators are visible at first glance.
2. Role-first navigation: primary navigation is grouped by role responsibilities before feature boundaries.
3. Action proximity: critical actions are placed next to decision data.
4. Progressive detail: users move from overview to focused workspaces without context loss.
5. Tenant-safe context: all views are tenant-scoped and inherit current auth/tenant context.

## Primary Role Areas

### Customer Care

Primary objective: triage, progress, and coordinate service requests.

Navigation priority order:

1. Queue Overview
2. Pipeline
3. Requests Workspace
4. Assignments
5. Communications

### Management

Primary objective: monitor operations, govern configuration, and manage workforce/system policy.

Navigation priority order:

1. Operations Overview
2. Workers
3. Reports
4. Subscriptions
5. Categories
6. Settings

## Navigation Model

### Global Level

- Portal Home
- Customer Care section
- Management section

### Section Level (Customer Care)

- Overview: high-level queue health and incoming volume signal.
- Pipeline: stage-based view of request flow and bottlenecks.
- Requests: searchable request list + request detail entry path.
- Assignments: assignment state and dispatch coordination surface.
- Communications: customer/worker communication timeline and pending follow-ups.

### Section Level (Management)

- Overview: executive operational snapshot and risk indicators.
- Workers: workforce list, availability status, and capacity controls.
- Reports: KPI and trend drill-ins.
- Subscriptions: plan and entitlement oversight.
- Categories: taxonomy and service domain structure.
- Settings: governance-level operational settings.

## Route and Workspace Mapping

Current route scaffolding in portal shell maps to IA as follows:

- `/` -> Portal Home
- `/customer-care` -> Customer Care Overview
- `/customer-care/pipeline` -> Customer Care Pipeline
- `/customer-care/requests` -> Requests Workspace
- `/customer-care/assignments` -> Assignments Workspace
- `/customer-care/communications` -> Communications Workspace
- `/management` -> Management Overview
- `/management/workers` -> Workforce Management
- `/management/reports` -> Reporting and KPI drill-in
- `/management/subscriptions` -> Subscription Operations
- `/management/categories` -> Category Governance
- `/management/settings` -> Governance Settings

## Screen Hierarchy

Each role workspace follows this hierarchy:

1. Snapshot row: key metrics and alert banners.
2. Work surface: main list/board/table for active operations.
3. Context panel: selected item details and action controls.
4. Activity rail: timeline/audit/recent changes where applicable.

## Cross-Cutting Information Objects

These objects must remain consistently represented across Customer Care and Management surfaces:

- Service Request: status, priority, age, assigned worker, SLA risk.
- Job Assignment: assignment state, owner, reassignment history.
- Worker Capacity: availability, load, and assignment queue.
- Operational Signals: backlog depth, breach risk, blocked work.
- Governance Signals: permission-sensitive actions, audit visibility.

## Role-Focused Navigation Priorities

### Customer Care priorities

- Fast triage over report depth.
- In-flight request state over historical views.
- Dispatch/assignment actions available directly from queue context.

### Management priorities

- Trend and capacity visibility over individual request triage.
- Governance and configuration controls separated from day-to-day triage surfaces.
- Drill-down path from KPI to operational entity list.

## Decision Rules for Future Pages

When adding a new Phase 5 web page:

1. Place it under Customer Care if the primary actor is support/dispatch operations.
2. Place it under Management if the primary actor is oversight/governance.
3. Avoid duplicate concepts across sections unless workflow intent differs by role.
4. Ensure each new page has a direct path from one parent section and a clear return path.

## Non-Goals for 5.1.1

- Final visual design system polish.
- Real-time data wiring and event synchronization.
- Full page-level CRUD behavior.

These are handled in later Phase 5 and Phase 7 tasks.
