# API Contracts Organizational Guide

## Overview

This document defines the folder structure and naming conventions for all API request/response DTOs shared across frontend (web, mobile) and backend services.

All API contracts are defined in the shared contracts library (`GTEK.FSM.Shared.Contracts`) to ensure single source of truth and type-safe consumption by all clients.

---

## Folder Structure

```
Api/
├── Requests/
│   └── [Common request base classes and utilities]
├── Responses/
│   ├── ApiResponse.cs           [Generic response envelope]
│   ├── ApiResponse.cs           [Non-generic response variant]
│   └── PaginationMetadata.cs    [Pagination info for list responses]
└── Contracts/
    ├── Requests/
    │   ├── GetRequestsRequest.cs
    │   └── [Other request-feature contracts]
    ├── [Feature Name]/
    │   ├── Requests/
    │   │   ├── GetJobsRequest.cs
    │   │   ├── CreateJobRequest.cs
    │   │   └── [Other job-specific requests]
    │   └── Responses/
    │       ├── GetJobsResponse.cs
    │       ├── CreateJobResponse.cs
    │       └── [Other job-specific responses]
    ├── Users/
    │   ├── Requests/
    │   │   ├── GetUsersRequest.cs
    │   │   └── [Other user-specific requests]
    │   └── Responses/
    │       ├── GetUsersResponse.cs
    │       └── [Other user-specific responses]
    └── [Additional features follow the same pattern]
```

---

## Naming Conventions

### Request DTOs

- **Pattern:** `[Verb][Plural/Singular Noun]Request`
- **Examples:**
  - `GetRequestsRequest` — list all requests with pagination
  - `CreateRequestRequest` — create a new request
  - `UpdateRequestRequest` — update an existing request
  - `DeleteRequestRequest` — delete a request
  - `GetJobsRequest` — list all jobs
  - `GetUsersRequest` — list all users

- **Guidelines:**
  - Use plural noun for list/collection requests (`GetRequestsRequest`)
  - Use singular noun for create/update/detail requests (`CreateRequestRequest`)
  - Always use `Request` suffix for clarity
  - Verb should match HTTP method intent (GET → Get, POST → Create, PUT → Update, DELETE → Delete)

### Response DTOs

- **Pattern:** `[Query/Action Description]Response` or `[Noun]Response`
- **Examples:**
  - `GetRequestsResponse` — single request in list context
  - `GetRequestDetailResponse` — full request in detail context
  - `CreateRequestResponse` — result of creating a request
  - `GetJobsResponse` — single job in list context
  - `GetUsersResponse` — single user in list context

- **Guidelines:**
  - Always use `Response` suffix for clarity
  - Match response DTO to the corresponding request (e.g., `GetRequestsRequest` returns `List<GetRequestsResponse>`)
  - Include only fields necessary for the specific endpoint (no full entities)

---

## Common Patterns

### 1. Pagination Pattern

All list endpoints follow this pattern:

**Request:**
```csharp
public class GetRequestsRequest
{
    public int Offset { get; set; } = 0;
    public int Limit { get; set; } = 10;
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    // Optional filters
}
```

**Response (single item):**
```csharp
public class GetRequestsResponse
{
    public string? RequestId { get; set; }
    // ... fields specific to request summary
    public PaginationMetadata? Pagination { get; set; }
}
```

**Backend Controller:**
```csharp
[HttpGet]
public async Task<ApiResponse<List<GetRequestsResponse>>> GetRequests(
    [FromQuery] GetRequestsRequest request)
{
    var items = await _service.GetRequestsAsync(request);
    return new ApiResponse<List<GetRequestsResponse>>
    {
        IsSuccess = true,
        Data = items,
        Message = "Requests retrieved successfully"
    };
}
```

### 2. Create/Update Pattern

**Request:**
```csharp
public class CreateRequestRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    // ... required fields for creation
}
```

**Response:**
```csharp
public class CreateRequestResponse
{
    public string? RequestId { get; set; }
    public DateTime CreatedUtc { get; set; }
    // ... confirmation fields
}
```

### 3. Detail/Get Pattern

**Request:**
```csharp
public class GetRequestDetailRequest
{
    public string? RequestId { get; set; }
}
```

**Response:**
```csharp
public class GetRequestDetailResponse
{
    public string? RequestId { get; set; }
    // ... all fields for full request details
    public List<object>? RelatedJobs { get; set; }
}
```

---

## Base Response Envelope

All API responses are wrapped in `ApiResponse<T>` for consistency:

```csharp
public class ApiResponse<T>
{
    public bool IsSuccess { get; set; }           // Success indicator
    public string? Message { get; set; }          // User-facing message
    public T? Data { get; set; }                  // Typed payload
    public DateTime TimestampUtc { get; set; }   // Response timestamp
}
```

**Backend middleware automatically wraps all responses** so clients always receive this consistent envelope shape.

---

## Vocabulary References

Request and response DTOs frequently reference shared vocabulary enums:

- `UserRole` — Guest, Customer, Worker, Support, Manager, Admin
- `RequestStage` — New, Assigned, InProgress, OnHold, Completed, Cancelled
- `SubscriptionTier` — Free, Professional, Enterprise
- `AvailabilityStatus` — Available, Busy, OffDuty, OnLeave

When filtering or exposing role/stage/tier/status, store as `string?` property and reference the enum value by name in documentation:

```csharp
public class GetRequestsRequest
{
    /// <summary>
    /// Optional filter by request stage (e.g., "New", "Assigned", "InProgress").
    /// References RequestStage vocabulary.
    /// </summary>
    public string? StageFilter { get; set; }
}
```

---

## Feature Organization

Each feature (Requests, Jobs, Users, etc.) is organized in its own folder:

```
Api/Contracts/[FeatureName]/
├── Requests/
│   ├── Get[Feature]Request.cs
│   ├── Create[Feature]Request.cs
│   ├── Update[Feature]Request.cs
│   └── ...
└── Responses/
    ├── Get[Feature]Response.cs
    ├── Create[Feature]Response.cs
    ├── ...
```

This structure:
- **Scales**: New features can be added without impacting existing structure
- **Reduces naming conflicts**: Each feature has its own query/action request/response pairs
- **Simplifies discovery**: Clients know exactly where to find contracts for a given feature

---

## Usage in Frontend Clients

### Web Portal (Blazor)

```csharp
@inject HttpClient Http

// Inject request contract
var request = new GetRequestsRequest { Offset = 0, Limit = 10 };

// Call backend endpoint
var response = await Http.GetFromJsonAsync<ApiResponse<List<GetRequestsResponse>>>(
    $"api/v1/requests?offset={request.Offset}&limit={request.Limit}");

// Type-safe consumption
if (response?.IsSuccess == true)
{
    foreach (var item in response.Data ?? new())
    {
        Console.WriteLine($"{item.Summary} - {item.Stage}");
    }
}
```

### Mobile App (.NET MAUI)

```csharp
// Same DTOs, same response shape
var response = await _httpClient.GetFromJsonAsync<ApiResponse<List<GetJobsResponse>>>(
    "api/v1/jobs");

if (response?.IsSuccess == true)
{
    foreach (var job in response.Data ?? new())
    {
        displayItems.Add(job);
    }
}
```

---

## Future Expansion

As the platform grows, new features will follow this pattern consistently:

- **Phase 1 Features:** Requests, Jobs, Users (defined now)
- **Phase 2 Features:** Tenants, Subscriptions, Availability/Schedules
- **Phase 3 Features:** Notifications, Reports, Webhooks

Each feature adds its own:
- `Contracts/[FeatureName]/Requests/` folder with request DTOs
- `Contracts/[FeatureName]/Responses/` folder with response DTOs
- Documentation in the feature's README or architecture guide

---

## Rules & Best Practices

1. **Single Responsibility:** Request/response DTOs contain only data needed for that specific endpoint. Do not reuse the same DTO across multiple endpoints if their payloads differ.

2. **Namespacing:** Always place DTOs in the correct namespace:
   - `GTEK.FSM.Shared.Contracts.Api.Contracts.[FeatureName].Requests`
   - `GTEK.FSM.Shared.Contracts.Api.Contracts.[FeatureName].Responses`

3. **Documentation:** Every DTO and property must include XML documentation (`/// <summary>`) explaining its purpose and any validation rules.

4. **Timestamps:** Always use UTC (`DateTime.UtcNow`) for all timestamp properties. Property naming: `CreatedUtc`, `UpdatedUtc`, etc.

5. **Nullability:** Use nullable types (`?`) for optional properties. Initialize collections to empty lists, not null.

6. **Immutability:** Request/response DTOs should be simple data holders, not contain methods. Keep them immutable where possible.

7. **Consistency:** Request filters follow `[PropertyName]Filter` pattern. Sort parameters use `SortBy` and `SortDirection`.

8. **API Response Envelope:** All responses must be wrapped in `ApiResponse<T>` or `ApiResponse` (non-generic).

---

## Example: Adding a New Feature Contract

When adding a new feature (e.g., `Support Tickets`):

1. Create folders:
   ```
   Api/Contracts/SupportTickets/Requests/
   Api/Contracts/SupportTickets/Responses/
   ```

2. Add request DTOs following naming convention:
   ```csharp
   public class GetSupportTicketsRequest { ... }
   public class CreateSupportTicketRequest { ... }
   ```

3. Add response DTOs matching each request:
   ```csharp
   public class GetSupportTicketsResponse { ... }
   public class CreateSupportTicketResponse { ... }
   ```

4. Include unit tests in backend to verify deserialization.

5. Update this guide with the new feature section.

---

## Related Resources

- [Shared Vocabulary Documentation](../Vocabulary/VOCABULARY.md)
- Backend API versioning and routing: `backend/api/Program.cs`
- Existing endpoints following this pattern: `backend/api/Controllers/`
