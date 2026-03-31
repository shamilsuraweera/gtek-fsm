using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.ServiceRequests;
using Microsoft.AspNetCore.Http;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;
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

        v1.MapPost("/requests/{requestId:guid}/assign", async (
            Guid requestId,
            AssignServiceRequestRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IServiceRequestAssignmentService assignmentService,
            CancellationToken cancellationToken) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal is null)
            {
                return BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required.");
            }

            if (!principal.IsInRole("Support") && !principal.IsInRole("Manager") && !principal.IsInRole("Admin"))
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "AUTH_FORBIDDEN_ROLE", "Only customer care roles can assign workers.");
            }

            var resolvedTenantId = tenantContextAccessor.GetCurrentTenantId();
            if (!resolvedTenantId.HasValue || resolvedTenantId.Value != principal.TenantId)
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "TENANT_OWNERSHIP_MISMATCH", "Tenant ownership validation failed.");
            }

            var assignment = await assignmentService.AssignAsync(principal, requestId, request.WorkerUserId, cancellationToken);
            if (!assignment.IsSuccess || assignment.Payload is null)
            {
                return BuildFailure(
                    context,
                    assignment.StatusCode ?? StatusCodes.Status400BadRequest,
                    assignment.ErrorCode ?? "REQUEST_ASSIGNMENT_FAILED",
                    assignment.Message);
            }

            var payload = new ServiceRequestAssignmentResponse
            {
                RequestId = assignment.Payload.RequestId.ToString(),
                TenantId = assignment.Payload.TenantId.ToString(),
                JobId = assignment.Payload.JobId.ToString(),
                PreviousWorkerUserId = assignment.Payload.PreviousWorkerUserId?.ToString(),
                CurrentWorkerUserId = assignment.Payload.CurrentWorkerUserId.ToString(),
                AssignmentStatus = assignment.Payload.AssignmentStatus,
                UpdatedAtUtc = assignment.Payload.UpdatedAtUtc,
            };

            var envelope = ApiResponse<ServiceRequestAssignmentResponse>.Ok(
                data: payload,
                message: assignment.Message,
                traceId: context.TraceIdentifier);

            return Results.Ok(envelope);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SupportFlow);

        v1.MapPost("/requests/{requestId:guid}/reassign", async (
            Guid requestId,
            ReassignServiceRequestRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IServiceRequestAssignmentService assignmentService,
            CancellationToken cancellationToken) =>
        {
            var principal = principalAccessor.GetCurrent();
            if (principal is null)
            {
                return BuildFailure(context, StatusCodes.Status401Unauthorized, "AUTH_UNAUTHORIZED", "Authentication is required.");
            }

            if (!principal.IsInRole("Support") && !principal.IsInRole("Manager") && !principal.IsInRole("Admin"))
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "AUTH_FORBIDDEN_ROLE", "Only customer care roles can reassign workers.");
            }

            var resolvedTenantId = tenantContextAccessor.GetCurrentTenantId();
            if (!resolvedTenantId.HasValue || resolvedTenantId.Value != principal.TenantId)
            {
                return BuildFailure(context, StatusCodes.Status403Forbidden, "TENANT_OWNERSHIP_MISMATCH", "Tenant ownership validation failed.");
            }

            var assignment = await assignmentService.ReassignAsync(principal, requestId, request.WorkerUserId, cancellationToken);
            if (!assignment.IsSuccess || assignment.Payload is null)
            {
                return BuildFailure(
                    context,
                    assignment.StatusCode ?? StatusCodes.Status400BadRequest,
                    assignment.ErrorCode ?? "REQUEST_REASSIGNMENT_FAILED",
                    assignment.Message);
            }

            var payload = new ServiceRequestAssignmentResponse
            {
                RequestId = assignment.Payload.RequestId.ToString(),
                TenantId = assignment.Payload.TenantId.ToString(),
                JobId = assignment.Payload.JobId.ToString(),
                PreviousWorkerUserId = assignment.Payload.PreviousWorkerUserId?.ToString(),
                CurrentWorkerUserId = assignment.Payload.CurrentWorkerUserId.ToString(),
                AssignmentStatus = assignment.Payload.AssignmentStatus,
                UpdatedAtUtc = assignment.Payload.UpdatedAtUtc,
            };

            var envelope = ApiResponse<ServiceRequestAssignmentResponse>.Ok(
                data: payload,
                message: assignment.Message,
                traceId: context.TraceIdentifier);

            return Results.Ok(envelope);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SupportFlow);

        v1.MapGet("/requests", async (
            [AsParameters] GetRequestsRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IServiceRequestQueryService requestQueryService,
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

            var query = await requestQueryService.QueryAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "REQUEST_QUERY_FAILED",
                    query.Message);
            }

            var page = query.Payload;
            var response = new GetRequestsListResponse
            {
                Items = page.Items
                    .Select(x => new GetRequestsResponse
                    {
                        RequestId = x.RequestId.ToString(),
                        Stage = x.Status,
                        Summary = x.Summary,
                        TenantId = x.TenantId.ToString(),
                        CreatedByRole = "Customer",
                        CreatedUtc = x.CreatedAtUtc,
                        UpdatedUtc = x.UpdatedAtUtc,
                    })
                    .ToArray(),
                Pagination = new GTEK.FSM.Shared.Contracts.Api.Responses.PaginationMetadata
                {
                    Offset = (page.Page - 1) * page.PageSize,
                    Limit = page.PageSize,
                    Total = page.Total,
                },
            };

            var envelope = ApiResponse<GetRequestsListResponse>.Ok(
                data: response,
                message: query.Message,
                traceId: context.TraceIdentifier);

            return Results.Ok(envelope);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        v1.MapGet("/jobs", async (
            [AsParameters] GetJobsRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IJobQueryService jobQueryService,
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

            var query = await jobQueryService.QueryAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "JOB_QUERY_FAILED",
                    query.Message);
            }

            var page = query.Payload;
            var response = new GetJobsListResponse
            {
                Items = page.Items
                    .Select(x => new GetJobsResponse
                    {
                        JobId = x.JobId.ToString(),
                        Title = x.Title,
                        Status = x.Status,
                        RequestId = x.RequestId.ToString(),
                        AssignedTo = x.AssignedTo?.ToString(),
                        AssignedUtc = x.AssignedUtc,
                    })
                    .ToArray(),
                Pagination = new GTEK.FSM.Shared.Contracts.Api.Responses.PaginationMetadata
                {
                    Offset = (page.Page - 1) * page.PageSize,
                    Limit = page.PageSize,
                    Total = page.Total,
                },
            };

            var envelope = ApiResponse<GetJobsListResponse>.Ok(
                data: response,
                message: query.Message,
                traceId: context.TraceIdentifier);

            return Results.Ok(envelope);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

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
