# Security Developer Runbook

**Phase 2.4.5 Artifact** — Local troubleshooting guide for authentication, role mapping, and tenant resolution failures.

## Overview

This runbook provides step-by-step procedures for developers to debug and resolve authentication, authorization, and tenant-scoping issues in local and development environments.

**Audience**: Backend and full-stack developers troubleshooting identity/access failures.  
**Scope**: Local development setup through integration testing; does not cover production incident response.

---

## Part 1: Quick Diagnosis Checklist

Use this checklist to identify the failure category before diving into detailed troubleshooting.

### Is the request failing with `401 Unauthorized`?

- [ ] Token is missing or malformed
- [ ] Token is expired or invalid signature
- [ ] Token issuer/audience mismatch with local config
- [ ] JWT bearer authentication handler not wired in middleware
- **→ See:** JWT Token Validation section below

### Is the request failing with `403 Forbidden`?

- [ ] Role/permission check failed
- [ ] Tenant context missing or unresolved
- [ ] Tenant context mismatch (claim vs header vs context)
- [ ] Policy requirement not satisfied
- **→ See:** Role and Permission Validation or Tenant Resolution sections below

### Is the request returning wrong data or cross-tenant leakage?

- [ ] Repository query not filtering by tenant
- [ ] Tenant context not propagated to repository layer
- [ ] Authenticated principal not available to handler
- **→ See:** Tenant Context Propagation section below

### Is a specific middleware step failing?

- [ ] `UseAuthentication` not placed before routing/authorization
- [ ] `UseTenantResolution` not placed between authentication and authorization
- [ ] Missing middleware registration in DependencyInjection
- **→ See:** Middleware Configuration section below

---

## Part 2: Detailed Troubleshooting Guides

### JWT Token Validation

**Symptoms**:

- `401 Unauthorized` response on all protected endpoints
- Error message: `The token was not recognized as a valid JWT`
- Token accepted by some endpoints but not others

#### Step 1: Verify JWT Configuration

Check `backend/api/Middleware/JwtBearerConfiguration.cs` or the composition root for:

````bash
cd backend/api
grep -r "JwtBearerDefaults\|AddAuthentication" --include="*.cs" | head -20
```text

Expected output should show `AddJwtBearer` with:

- Issuer (e.g., `https://localhost:5001`)
- Audience (e.g., `gtek-fsm-api`)
- Key for signature validation (from environment or config)

**Common Config Issues**:

| Issue | Check | Fix |
| ------- | ------- | ----- |
| `TokenValidationFailed` | Token issuer doesn't match config | Update `JwtBearerOptions.TokenValidationParameters.ValidIssuer` |
| `InvalidOperationException` signing key | No signing key configured | Add signing key to `TokenValidationParameters.IssuerSigningKey` |
| Token works elsewhere but not locally | Audience mismatch | Ensure token audience matches `ValidAudience` in local config |

##### Step 2: Generate Test Token

If configuration is correct but token generation is the issue:

```bash
# Navigate to token generation script or testbed
cd backend/api

# Check if token helper exists
find . -name "*Token*" -o -name "*Jwt*" | grep -i test

# Look for local token generation in test project
find ../infrastructure.tests -name "*TokenClaimNames*" -o -name "*TestAuth*"
````

##### Manual Token Generation

If no script exists, use [jwt.io](https://jwt.io) or a token generator in your test project:

1. Header:

````json
{
  "alg": "HS256",
  "typ": "JWT"
}
```text

2. Payload (adjust claims per role):

```json
{
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "tenant_id": "660e8400-e29b-41d4-a716-446655440001",
  "role": "Admin",
  "iat": 1700000000,
  "exp": 1800000000,
  "iss": "https://localhost:5001",
  "aud": "gtek-fsm-api"
}
```

1. Issuer and audience match local config
2. Claims map to your application expectations

Send token in Authorization header:

````bash
TOKEN="your_jwt_token_here"

curl -v http://localhost:7071/ping \
  -H "Authorization: Bearer $TOKEN"
```text

Expected responses:

- `200 OK` → Token is valid
- `401 Unauthorized` → See middleware/config issues below
- `403 Forbidden` → Token valid but missing required claims/permissions

**Token Claim Validation**:

If using a test harness or logging, verify token contains:

```csharp
// In your test or logged output, verify:
var claims = principal.Claims;
claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value  // sub
claims.FirstOrDefault(c => c.Type == TokenClaimNames.TenantId)?.Value   // tenant_id
claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value            // role
````

---

### Middleware Configuration

**Symptoms**:

- Requests skip authentication entirely
- `401` only when endpoint has `RequireAuthorization()`
- Tenant resolution bypassed for some requests

**Check Middleware Order**:

In `backend/api/Program.cs` or `Startup.cs`, middleware must be in this order:

````csharp
// CORRECT ORDER:
app.UseAuthentication();        // 1. Parse JWT and extract claims
app.UseTenantResolution();      // 2. Resolve tenant from claims/header
app.UseAuthorization();         // 3. Check roles and policies
app.MapControllers();           // 4. Route to handlers
app.MapGet(...);                // 5. Handle requests
```text

**Verification**:

```bash
cd backend/api
grep -n "app.Use\|app.Map" Program.cs | head -30
````

Look for the ordering above. If `UseTenantResolution()` comes after `UseAuthorization()`, tenant context won't be available during policy evaluation.

**Register Middleware**:

If middleware is missing from the pipeline:

````csharp
// In DependencyInjection.cs
services.AddScoped<TenantResolutionMiddleware>();

// In Program.cs
app.UseMiddleware<TenantResolutionMiddleware>();
```text

**Check Middleware Dependencies**:

Ensure all required services are registered:

```bash
grep -r "AddScoped.*ITenantContextAccessor\|IAuthenticatedPrincipalAccessor" backend/api --include="*.cs"
````

If not found, add to DependencyInjection:

````csharp
services.AddScoped<ITenantContextAccessor, TenantContextAccessor>();
services.AddScoped<IAuthenticatedPrincipalAccessor, AuthenticatedPrincipalAccessor>();
```text

---

### Role and Permission Validation

**Symptoms**:

- `403 Forbidden` on endpoints that should be allowed
- Role checks passing locally but failing in CI
- Log shows `permission_insufficient` audit reason

**Step 1: Verify Token Contains Role Claim**

Check token has role claim:

```bash
# Decode JWT payload at jwt.io or programmatically:
echo "your_token" | cut -d'.' -f2 | base64 -d | jq '.role'
# Expected: "Admin" or comma-separated roles like "Admin,Manager"
```

##### Step 2: Check Permission Matrix

Verify role-to-permission mapping:

````bash
cd backend/application

# Find permission definitions
find . -name "*Permission*" | xargs grep -l "const string\|public static"

# Check role authorizer
grep -r "RolePermissionAuthorizer\|IsAuthorizedForPermission" backend --include="*.cs" | head -5
```text

Example permission check:

```csharp
// In RolePermissionAuthorizer or similar
var isAuthorized = RolePermissionAuthorizer.IsAuthorizedForPermission(
    userRoles: "Admin",            // from token
    requiredPermission: "TenantsRead"
);
````

**Verify Mapping**:

````bash
# Find permission matrix
cd backend/application
grep -A 30 "case.*Admin:" Identity/RolePermissionAuthorizer.cs | head -40
```text

Expected matrix:

````

Admin: [TenantsRead, TenantsWrite, UsersRead, UsersWrite, ...]
Manager: [UsersRead, RequestsRead, ...]

````text

**Step 3: Check Endpoint Policy**

Verify the endpoint defines the correct policy:

```bash
cd backend/api

# Find protected endpoints
grep -B3 "RequireAuthorization\|Authorize" --include="*.cs" -r | head -30
````

Look for:

````csharp
app.MapGet("/api/v1/tenants", handler)
   .RequireAuthorization(AuthorizationPolicyCatalog.TenantsRead);
```text

**Policy Doesn't Match Permission**:

If endpoint requires `TenantsRead` but token role doesn't grant it:

1. Issue: Role mapping incomplete
   - **Fix**: Add role to permission matrix in `RolePermissionAuthorizer`

2. Issue: Token has wrong role
   - **Fix**: Regenerate test token with correct role

3. Issue: Policy name typo
   - **Fix**: Match `RequireAuthorization("policy_name")` with defined policy in `AddAuthorizationBuilder()`

**Step 4: Test with Different Roles**

Create test tokens for each role and verify expected allow/deny:

```bash
# Admin token - should pass TenantsRead
curl -H "Authorization: Bearer admin_token" http://localhost:7071/api/v1/tenants

# Worker token - should fail TenantsRead (403)
curl -H "Authorization: Bearer worker_token" http://localhost:7071/api/v1/tenants  # Expect 403
````

---

### Tenant Resolution

**Symptoms**:

- `401 Unauthorized` with message `TENANT_CONTEXT_UNRESOLVED`
- `403 Forbidden` with reason `CROSS_TENANT_FORBIDDEN`
- Tenant header ignored or invalid

#### Step 1: Verify Tenant Claim in Token

Token must contain `tenant_id` claim:

````bash
# Decode token
echo "your_token" | cut -d'.' -f2 | base64 -d | jq '.tenant_id'
# Expected: valid GUID like "660e8400-e29b-41d4-a716-446655440001"
```text

If missing, regenerate token with tenant claim.

**Step 2: Check Tenant Resolution Policy**

In middleware or configuration, verify tenant resolution order:

```bash
cd backend/api

# Find TenantResolutionPolicy
grep -r "TenantResolutionPolicy\|Resolve" --include="*.cs" | grep -i tenant
````

Expected logic:

````csharp
// Priority order:
// 1. tenant_id claim from JWT
if (claimTenant != null) return claimTenant;

// 2. X-Tenant-Id header (only if allowed role)
if (CanUseHeaderFallback(principal, allowedRoles))
    return headerTenant;

// 3. Fail if neither
return Unresolved;
```text

**Common Issues**:

| Issue | Symptom | Check | Fix |
| ------- | --------- | ------- | ----- |
| Claim missing | `TENANT_CONTEXT_UNRESOLVED` | Token has `tenant_id` claim | Regenerate token |
| Header required but not sent | `TENANT_CONTEXT_UNRESOLVED` | Request has `X-Tenant-Id` header | Add header or use token claim |
| Header not allowed for role | `TENANT_CONTEXT_UNRESOLVED` | Role in `HeaderFallbackAllowedRoles` | Add role to allowed list or use token claim |
| Mismatch (claim ≠ header) | `CROSS_TENANT_FORBIDDEN` | Claim and header match | Use consistent tenant ID or remove header |

**Step 3: Validate Tenant Context Accessor**

Verify tenant context is available in handler:

```csharp
// In endpoint handler
var tenantId = tenantContextAccessor.GetCurrentTenantId();
if (!tenantId.HasValue)
{
    // Should not reach here if middleware succeeded
    // If it does, middleware registration issue
    return Results.BadRequest("Tenant context not resolved by middleware");
}
````

##### Step 4: Test Scenarios

````bash
# Scenario A: Tenant in token claim (preferred)
curl -H "Authorization: Bearer $TOKEN_WITH_TENANT" \
     http://localhost:7071/api/v1/tenants
# Expected: 200 or 403 based on permissions, NOT 401

# Scenario B: Tenant in header (fallback)
curl -H "Authorization: Bearer $TOKEN_WITH_TENANT" \
     -H "X-Tenant-Id: 660e8400-e29b-41d4-a716-446655440001" \
     http://localhost:7071/api/v1/tenants
# Expected: 200 or 403; 401 if fallback not allowed for role

# Scenario C: Cross-tenant attempt (should fail)
TOKEN_CLAIM_TENANT="660e8400-e29b-41d4-a716-446655440001"
HEADER_TENANT="770e8400-e29b-41d4-a716-446655440002"

curl -H "Authorization: Bearer $TOKEN_claim_tenant" \
     -H "X-Tenant-Id: $HEADER_TENANT" \
     http://localhost:7071/api/v1/tenants
# Expected: 403 if cross-tenant not allowed; 401 if privileged flow required
```text

---

### Tenant Context Propagation

**Symptoms**:

- Endpoint returns data from other tenants
- Query results not filtered by authenticated tenant
- Repository not respecting tenant context

**Step 1: Verify Authenticated Principal is Available**

In endpoint handler, check principal is resolved:

```csharp
public async Task<IResult> GetUsers(
    IAuthenticatedPrincipalAccessor principalAccessor,
    IUserRepository userRepository)
{
    var principal = principalAccessor.GetCurrent();
    if (principal?.TenantId == null)
    {
        // Middleware should have rejected this earlier
        // If we reach here, middleware chain is broken
        return Results.BadRequest("Authenticated principal not available");
    }

    // Repository method should use principal.TenantId
}
````

##### Step 2: Check Repository Query Path

Verify repository applies tenant filter:

````bash
cd backend/infrastructure

# Find repository implementation
grep -r "ListByTenantAsync\|ApplyTenantFilter" --include="*.cs" | head -10
```text

Expected pattern:

```csharp
public async Task<List<User>> ListByTenantAsync(Guid tenantId, CancellationToken ct)
{
    return await _context.Users
        .Where(u => u.TenantId == tenantId)  // ESSENTIAL: filter by tenant
        .ToListAsync(ct);
}
````

**Verify Filter is Applied**:

If repository queries don't filter:

1. Add tenant predicate:

````csharp
.Where(u => u.TenantId == tenantId)
```text

2. Test with multi-tenant data:

```csharp
// Seed 2 users in different tenants
var user1 = new User { TenantId = tenant1, Name = "User1" };
var user2 = new User { TenantId = tenant2, Name = "User2" };

// Query tenant1
var results = await repo.ListByTenantAsync(tenant1, ct);

// Should return only user1, not user2
Assert.Single(results);  // ✓ Pass
Assert.Contains(user1, results);
```

##### Step 3: Check Context Lifetime

Verify tenant context is available across async boundaries:

````csharp
// Using HttpContext.Items loses context in background tasks
app.MapGet("/api/v1/users", async (
    HttpContext context,
    IUserRepository repo) =>
{
    // ✓ OK: Tenant context from middleware is in HttpContext.Items
    var tenantId = (Guid)context.Items[TenantContextConstants.HttpContextItemKey];
    var users = await repo.ListByTenantAsync(tenantId, ct);
    return Results.Ok(users);
});

// ✗ BROKEN: Background task loses context
app.MapGet("/api/v1/batch-process", async (HttpContext context) =>
{
    var tenantId = (Guid)context.Items[TenantContextConstants.HttpContextItemKey];

    #pragma warning disable CS4014  // Don't await on purpose
    Task.Run(async () =>
    {
        // ✗ TenantContextAccessor will return null here
        var tenant = tenantContextAccessor.GetCurrentTenantId();  // NULL!
    });
    #pragma warning restore CS4014

    return Results.Accepted();
});
```text

**Fix for Background Tasks**:

Pass tenant context explicitly:

```csharp
app.MapGet("/api/v1/batch-process", async (
    IAuthenticatedPrincipalAccessor principalAccessor,
    ITenantContextAccessor tenantAccessor) =>
{
    var principal = principalAccessor.GetCurrent();
    var tenantId = tenantAccessor.GetCurrentTenantId();

    // Capture values before background task
    #pragma warning disable CS4014
    Task.Run(async () =>
    {
        using var scope = serviceProvider.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        // Use captured values, not accessor
        await repo.ListByTenantAsync(tenantId.Value, CancellationToken.None);
    });
    #pragma warning restore CS4014

    return Results.Accepted();
});
````

---

## Part 3: Audit Logging for Diagnostics

Enable detailed audit and trace logging to diagnose failures:

**In `appsettings.Development.json`**:

````json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "GTEK.FSM.Backend.Api.Middleware": "Debug",
      "GTEK.FSM.Backend.Api.Authorization": "Debug",
      "GTEK.FSM.Backend.Application.Identity": "Debug"
    }
  }
}
```text

**Watch Audit Events**:

```bash
# In local logs, look for:
authorization_decision action=permission_check outcome=\* reason=\*

# Filter by outcome
# Allowed decisions:
authorization_decision action=permission_check outcome=allowed reason=permission_granted

# Denied decisions:
authorization_decision action=permission_check outcome=denied reason=permission_insufficient
````

**Example Audit Log Entry**:

```text
authorization_decision action=permission_check:TenantsRead outcome=denied reason=permission_insufficient userId=550e8400-e29b-41d4-a716-446655440000 sourceTenantId=660e8400-e29b-41d4-a716-446655440001 targetTenantId=null occurredAtUtc=2026-03-20T14:30:45.1234567Z
```

---

## Part 4: Integration Test Procedures

Validate end-to-end flows before committing:

**Run Identity & Authorization Tests**:

````bash
cd backend

# Phase 2 integration tests
dotnet test infrastructure.tests/Integration/AuthTenantIsolationIntegrationTests.cs -v

# Audit logging tests
dotnet test infrastructure.tests/Identity/StructuredAuditFieldsTests.cs -v

# Query-path regression tests
dotnet test infrastructure.tests/Integration/AuthenticatedTenantQueryPathIntegrationTests.cs -v
```text

**Verify All Pass**:

Expected output:

````

5 passed in 2.3s

````text

If tests fail, refer to the failure message and cross-reference the section above.

---

## Part 5: Quick Reference

### Headers and Claims for Local Testing

**Test Token Structure**:

```json
Header: { "alg": "HS256", "typ": "JWT" }
Payload: {
  "sub": "550e8400-e29b-41d4-a716-446655440000",
  "tenant_id": "660e8400-e29b-41d4-a716-446655440001",
  "role": "Admin",
  "iat": 1700000000,
  "exp": 1800000000,
  "iss": "https://localhost:5001",
  "aud": "gtek-fsm-api"
}
````

**Request Headers**:

````bash
# With bearer token
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# With tenant header (fallback)
X-Tenant-Id: 660e8400-e29b-41d4-a716-446655440001
```text

### Common Error Messages

| Error | Likely Cause | Fix |
| ------- | -------------- | ----- |
| `401 Unauthorized` | Missing/invalid token or JWT config mismatch | Verify token format and JWT config |
| `403 Forbidden` | Permission check failed | Check role-permission mapping |
| `TENANT_CONTEXT_UNRESOLVED` | Tenant not in token or header | Add `tenant_id` claim to token |
| `CROSS_TENANT_FORBIDDEN` | Attempt to access different tenant | Use same tenant in token and header |
| `permission_insufficient` | Role lacks required permission | Check RolePermissionAuthorizer matrix |

---

## Part 6: When to Escalate

Contact the security/platform team if:

- JWT signing key is compromised or needs rotation
- New roles or permissions need to be added to the matrix
- Tenant isolation bypass suspected (cross-tenant data leakage despite fixes)
- Production incident affects authentication/authorization
- Audit trail shows suspicious patterns (repeated failures, privilege escalation attempts)

---

**Document Version**: 1.0
**Last Updated**: Phase 2.4.5
**Related Artifacts**:

- `backend/application/PHASE2_IDENTITY_ACCESS_PLAN.md`
- `backend/api/Middleware/JwtBearerConfiguration.cs`
- `backend/api/Authorization/PermissionAuthorizationHandler.cs`
- `backend/infrastructure.tests/Integration/AuthTenantIsolationIntegrationTests.cs`
````
