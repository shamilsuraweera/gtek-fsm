using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Results;

namespace GTEK.FSM.Backend.Api.Routing;

public static class V1RouteGroupExtensions
{
    public static IEndpointRouteBuilder MapV1Endpoints(this IEndpointRouteBuilder app)
    {
        // Business and feature endpoints register under versioned route groups.
        var v1 = app.MapGroup(ApiRouteConstants.V1);

        v1.MapGet("/ping", (HttpContext context) =>
            Results.Ok(ApiResponse<object>.Ok(
                data: new { status = "ok" },
                message: "API is reachable.",
                traceId: context.TraceIdentifier)));

        v1.MapGet("/error-test", () =>
        {
            throw new InvalidOperationException("Error test endpoint triggered.");
        });

        v1.MapPost("/requests", async (
            CreateServiceRequestRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IServiceRequestCreationService creationService,
            CancellationToken cancellationToken) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal is null)
            {
                return BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required.");
            }

            if (!principal.IsInRole("Customer"))
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "AUTH_FORBIDDEN_ROLE", "Only customers can create service requests.");
            }

            var resolvedTenantId = tenantContextAccessor.GetCurrentTenantId();
            if (!resolvedTenantId.HasValue || resolvedTenantId.Value != principal.TenantId)
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "TENANT_OWNERSHIP_MISMATCH", "Tenant ownership validation failed.");
            }

            var creation = await creationService.CreateAsync(principal, request.Title, cancellationToken);
            if (!creation.IsSuccess || creation.Payload is null)
            {
                return BuildFailure(
                    context,
                    StatusCodes.Status400BadRequest,
                    creation.ErrorCode ?? "VALIDATION_FAILED",
                    creation.Message);
            }

            var payload = new CreateServiceRequestResponse
            {
                RequestId = creation.Payload.RequestId.ToString(),
                TenantId = creation.Payload.TenantId.ToString(),
                CustomerUserId = creation.Payload.CustomerUserId.ToString(),
                Title = creation.Payload.Title,
                Status = creation.Payload.Status,
                CreatedAtUtc = creation.Payload.CreatedAtUtc,
                UpdatedAtUtc = creation.Payload.UpdatedAtUtc,
            };

            var envelope = ApiResponse<CreateServiceRequestResponse>.Ok(
                data: payload,
                message: creation.Message,
                traceId: context.TraceIdentifier);

            return Results.Json(envelope, statusCode: StatusCodes.Status201Created);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.CustomerFlow);

        v1.MapPatch("/requests/{requestId:guid}/status", async (
            Guid requestId,
            TransitionServiceRequestStatusRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IServiceRequestLifecycleService lifecycleService,
            CancellationToken cancellationToken) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal is null)
            {
                return BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required.");
            }

            var resolvedTenantId = tenantContextAccessor.GetCurrentTenantId();
            if (!resolvedTenantId.HasValue || resolvedTenantId.Value != principal.TenantId)
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "TENANT_OWNERSHIP_MISMATCH", "Tenant ownership validation failed.");
            }

            var transition = await lifecycleService.TransitionAsync(
                principal,
                requestId,
                request.NextStatus,
                cancellationToken);

            if (!transition.IsSuccess || transition.Payload is null)
            {
                return BuildFailure(
                    context,
                    transition.StatusCode ?? StatusCodes.Status400BadRequest,
                    transition.ErrorCode ?? "REQUEST_TRANSITION_FAILED",
                    transition.Message);
            }

            var payload = new TransitionServiceRequestStatusResponse
            {
                RequestId = transition.Payload.RequestId.ToString(),
                TenantId = transition.Payload.TenantId.ToString(),
                PreviousStatus = transition.Payload.PreviousStatus,
                CurrentStatus = transition.Payload.CurrentStatus,
                UpdatedAtUtc = transition.Payload.UpdatedAtUtc,
            };

            var envelope = ApiResponse<TransitionServiceRequestStatusResponse>.Ok(
                data: payload,
                message: transition.Message,
                traceId: context.TraceIdentifier);

            return Results.Ok(envelope);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.CustomerFlow);

        // Bootstrap probe endpoints for validating auth pipeline outcomes with standard envelopes.
        v1.MapGet("/auth/bootstrap/authenticated", (
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal is null)
            {
                return BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required.");
            }

            var payload = new
            {
                principal.UserId,
                principal.TenantId,
                ResolvedTenantId = tenantContextAccessor.GetCurrentTenantId(),
                Roles = principal.Roles.OrderBy(x => x).ToArray(),
                Scopes = principal.Scopes.OrderBy(x => x).ToArray(),
            };

            return Results.Ok(ApiResponse<object>.Ok(
                data: payload,
                message: "Authenticated context is valid.",
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        v1.MapGet("/auth/bootstrap/forbidden", (HttpContext context) =>
        {
            return Results.Ok(ApiResponse<object>.Ok(
                data: new { status = "allowed" },
                message: "Admin authorization granted.",
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.AdminFlow);

        v1.MapGet("/auth/bootstrap/unauthorized", (HttpContext context) =>
            BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required."));

        v1.MapGet("/tenant/{tenantId:guid}/ownership-check/read", (
            Guid tenantId,
            HttpContext context,
            ITenantOwnershipGuard tenantOwnershipGuard) =>
        {
            var ownership = tenantOwnershipGuard.EnsureTenantAccess(tenantId);
            if (!ownership.IsAllowed)
            {
                return BuildFailure(
                    context,
                    ownership.StatusCode ?? StatusCodes.Status403Forbidden,
                    ownership.ErrorCode ?? "TENANT_OWNERSHIP_MISMATCH",
                    ownership.Message ?? "Tenant ownership validation failed.");
            }

            return Results.Ok(ApiResponse<object>.Ok(
                data: new { tenantId, operation = "read" },
                message: "Tenant-scoped read boundary check passed.",
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.CustomerFlow);

        v1.MapPost("/tenant/{tenantId:guid}/ownership-check/write", (
            Guid tenantId,
            HttpContext context,
            ITenantOwnershipGuard tenantOwnershipGuard) =>
        {
            var ownership = tenantOwnershipGuard.EnsureTenantAccess(tenantId);
            if (!ownership.IsAllowed)
            {
                return BuildFailure(
                    context,
                    ownership.StatusCode ?? StatusCodes.Status403Forbidden,
                    ownership.ErrorCode ?? "TENANT_OWNERSHIP_MISMATCH",
                    ownership.Message ?? "Tenant ownership validation failed.");
            }

            return Results.Ok(ApiResponse<object>.Ok(
                data: new { tenantId, operation = "write" },
                message: "Tenant-scoped write boundary check passed.",
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.WorkerFlow);

        v1.MapPost("/management/cross-tenant/{tenantId:guid}/guarded-probe", async (
            Guid tenantId,
            HttpContext context,
            IPrivilegedTenantOperationGuard privilegedGuard,
            CancellationToken cancellationToken) =>
        {
            var decision = await privilegedGuard.EvaluateAsync(
                new PrivilegedTenantOperationRequest(
                    TargetTenantId: tenantId,
                    Action: "management.cross_tenant.guarded_probe"),
                cancellationToken);

            if (!decision.IsAllowed)
            {
                return BuildFailure(
                    context,
                    decision.StatusCode ?? StatusCodes.Status403Forbidden,
                    decision.ErrorCode ?? "CROSS_TENANT_FORBIDDEN",
                    decision.Message ?? "Cross-tenant management guard rejected operation.");
            }

            return Results.Ok(ApiResponse<object>.Ok(
                data: new { targetTenantId = tenantId, operation = "cross-tenant-managed-probe" },
                message: "Privileged management guard passed.",
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        // Operational endpoints (for example /health) remain outside versioned groups.
        return app;
    }

    private static IResult BuildFailure(HttpContext context, int statusCode, string errorCode, string message)
    {
        var payload = ApiResponse<object>.Fail(message: message, errorCode: errorCode, traceId: context.TraceIdentifier);
        return Results.Json(payload, statusCode: statusCode);
    }

}
