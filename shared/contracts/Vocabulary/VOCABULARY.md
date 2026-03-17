# GTEK FSM Shared Vocabulary

## Overview

This directory contains the core domain vocabulary for the GTEK FSM platform. These enums and concepts define the fundamental entities and state transitions used across all backend services, web portals, and mobile clients.

## Core Concepts

### UserRole

Defines the primary participant types in the system:

- **Guest**: Unauthenticated user with minimal permissions.
- **Customer**: Submits requests for service or work.
- **Worker**: Field-based participant who executes jobs and completes requests.
- **Support**: Support team member who manages requests, assignments, and escalations.
- **Manager**: Coordinator with oversight, reporting, and decision-making authority.
- **Admin**: Full system access for configuration, audit, and operational control.

**Usage**: Authorization decisions, UI role-based navigation, audit logging, and service access control.

### RequestStage

Represents the lifecycle progression of a service request:

- **New**: Created and awaiting assignment.
- **Assigned**: Assigned to a worker but not yet started.
- **InProgress**: Worker is actively working on the request.
- **OnHold**: Paused temporarily (awaiting materials, customer input, etc.).
- **Completed**: Work finished and customer/support has accepted the outcome.
- **Cancelled**: Request was abandoned before completion.

**Usage**: Workflow state machines, filtering, reporting, and SLA tracking.

### SubscriptionTier

Defines the service level and feature availability for tenants:

- **Free**: Limited feature set and capacity suitable for evaluation or small operations.
- **Professional**: Standard feature set and moderate capacity for ongoing operations.
- **Enterprise**: Full feature set with unlimited capacity and premium support.

**Usage**: Feature flag evaluations, quota enforcement, billing, and commercial logic.

### AvailabilityStatus

Tracks whether a worker or resource is available for new assignments:

- **Available**: Ready to accept new work.
- **Busy**: Currently working and cannot accept additional assignments.
- **OffDuty**: Scheduled off, not accepting work.
- **OnLeave**: Extended absence, unavailable for a defined period.

**Usage**: Assignment algorithms, worker filtering, scheduling, and real-time status dashboards.

## Consumption Patterns

All clients (backend services, web portal, mobile app) import these enums from `GTEK.FSM.Shared.Contracts.Vocabulary` to ensure consistent definitions across the platform. This approach:

- Guarantees a single source of truth for domain concepts.
- Simplifies version management and migration strategies.
- Enables type-safe patterns in API contracts and state machines.
- Supports safe round-trip serialization across all clients.

## Future Expansion

Additional concepts will be added to this namespace as the platform evolves, such as:

- `JobType`: Categories of work (e.g., Installation, Maintenance, Support).
- `Priority`: Request/job priority levels (e.g., Critical, High, Normal, Low).
- `NotificationChannel`: Delivery methods (e.g., Email, Push, SMS).
- `AuditEventType`: Categories of audit log entries.

All new concepts will follow the same pattern and be documented here.
