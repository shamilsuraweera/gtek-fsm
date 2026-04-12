# Phase 11.6 Backend Observability Baseline

Date: 2026-04-13
Scope: Task 11.6

## Objective

Establish a backend observability baseline with structured telemetry, correlation, metrics, and trace-ready instrumentation across API request handling, realtime publishing, and async authorization decision workflows.

## Implemented Baseline

### 1. API Correlation and Request Telemetry

- Middleware: `backend/api/Middleware/RequestObservabilityMiddleware.cs`
- Pipeline wiring: `backend/api/Program.cs`
- Extension: `backend/api/Middleware/MiddlewareExtensions.cs`

Behavior:

- Accepts incoming `X-Correlation-Id` when provided.
- Generates correlation id from request trace id when absent.
- Writes `X-Correlation-Id` to response headers for end-to-end propagation.
- Emits structured start/completion logs with method, path, status, and elapsed time.
- Records API metrics:
  - `api_requests_total`
  - `api_request_duration_ms`

### 2. Realtime Telemetry (SignalR Publish Path)

- Publisher: `backend/api/Realtime/SignalROperationalUpdatePublisher.cs`

Behavior:

- Emits structured realtime publish logs with event type, tenant id, outcome, and elapsed time.
- Records realtime metrics:
  - `realtime_publishes_total`
  - `realtime_publish_duration_ms`

### 3. Async/Security Decision Telemetry

- Audit sink: `backend/infrastructure/Identity/AuthorizationDecisionAuditLogger.cs`

Behavior:

- Existing structured decision logs retained.
- Added authorization decision metric:
  - `authorization_decisions_total`

## Tenant-Safe Telemetry Posture

- Logs avoid request body payloads and secret material.
- Request telemetry logs method/path/status/latency and correlation identifiers only.
- Metrics use tenant id as tag for scoped observability; no sensitive PII payload values are emitted.
- Security decision telemetry captures action/outcome/reason dimensions, not credential data.

## Validation Evidence

Runtime tests:

- `backend/infrastructure.tests/Runtime/RequestObservabilityMiddlewareRuntimeTests.cs`
  - Correlation id propagation when absent/present.
  - API request metric emission with tenant tag.

## Operational Consumption Guidance

- Dashboard starter views:
  - Request volume and p95 latency by method/path.
  - Realtime publish success/failure trend by event type.
  - Authorization decision volume by outcome and reason.
- Alert starter thresholds:
  - Elevated 5xx ratio with rising p95 request latency.
  - Realtime publish failure spikes.
  - Authorization deny/guard failures above baseline.
