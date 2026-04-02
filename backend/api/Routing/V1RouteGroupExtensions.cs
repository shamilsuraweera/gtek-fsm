using System.Text;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Audit;
using GTEK.FSM.Backend.Application.Categories;
using GTEK.FSM.Backend.Application.Reporting;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Backend.Application.Subscriptions;
using GTEK.FSM.Backend.Application.Workers;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
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
            IValidator<CreateServiceRequestRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
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
            IValidator<TransitionServiceRequestStatusRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var transition = await lifecycleService.TransitionAsync(
                principal,
                requestId,
                request.NextStatus,
                request.RowVersion,
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
                RowVersion = transition.Payload.RowVersion,
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
            IValidator<AssignServiceRequestRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var assignment = await assignmentService.AssignAsync(principal, requestId, request.WorkerUserId, request.RowVersion, cancellationToken);
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
                RowVersion = assignment.Payload.RowVersion,
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
            IValidator<ReassignServiceRequestRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var assignment = await assignmentService.ReassignAsync(principal, requestId, request.WorkerUserId, request.RowVersion, cancellationToken);
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
                RowVersion = assignment.Payload.RowVersion,
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
            IValidator<GetRequestsRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
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

        v1.MapGet("/requests/{requestId:guid}", async (
            Guid requestId,
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

            var query = await requestQueryService.GetDetailAsync(principal, requestId, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "REQUEST_DETAIL_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetServiceRequestDetailResponse
            {
                RequestId = query.Payload.RequestId.ToString(),
                RowVersion = query.Payload.RowVersion,
                TenantId = query.Payload.TenantId.ToString(),
                CustomerUserId = query.Payload.CustomerUserId.ToString(),
                Title = query.Payload.Title,
                Status = query.Payload.Status,
                CreatedAtUtc = query.Payload.CreatedAtUtc,
                UpdatedAtUtc = query.Payload.UpdatedAtUtc,
                ActiveJobId = query.Payload.ActiveJobId?.ToString(),
                AssignedWorkerUserId = query.Payload.AssignedWorkerUserId?.ToString(),
                ActiveJobStatus = query.Payload.ActiveJobStatus,
                Timeline = query.Payload.Timeline
                    .Select(x => new DetailTimelineItemResponse
                    {
                        EventType = x.EventType,
                        Message = x.Message,
                        OccurredAtUtc = x.OccurredAtUtc,
                        ActorUserId = x.ActorUserId?.ToString(),
                    })
                    .ToArray(),
            };

            return Results.Ok(ApiResponse<GetServiceRequestDetailResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        v1.MapGet("/jobs", async (
            [AsParameters] GetJobsRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<GetJobsRequest> validator,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
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

        v1.MapGet("/jobs/{jobId:guid}", async (
            Guid jobId,
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

            var query = await jobQueryService.GetDetailAsync(principal, jobId, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "JOB_DETAIL_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetJobDetailResponse
            {
                JobId = query.Payload.JobId.ToString(),
                TenantId = query.Payload.TenantId.ToString(),
                RequestId = query.Payload.ServiceRequestId.ToString(),
                AssignmentStatus = query.Payload.AssignmentStatus,
                AssignedWorkerUserId = query.Payload.AssignedWorkerUserId?.ToString(),
                CreatedAtUtc = query.Payload.CreatedAtUtc,
                UpdatedAtUtc = query.Payload.UpdatedAtUtc,
                RequestTitle = query.Payload.RequestTitle,
                RequestStatus = query.Payload.RequestStatus,
                Timeline = query.Payload.Timeline
                    .Select(x => new DetailTimelineItemResponse
                    {
                        EventType = x.EventType,
                        Message = x.Message,
                        OccurredAtUtc = x.OccurredAtUtc,
                        ActorUserId = x.ActorUserId?.ToString(),
                    })
                    .ToArray(),
            };

            return Results.Ok(ApiResponse<GetJobDetailResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        v1.MapGet("/categories", async (
            bool? includeDisabled,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            ICategoryQueryService categoryQueryService,
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

            var query = await categoryQueryService.GetCategoriesAsync(principal, includeDisabled ?? false, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "CATEGORY_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetCategoriesListResponse
            {
                Items = query.Payload.Select(x => new CategoryResponse
                {
                    CategoryId = x.CategoryId.ToString(),
                    TenantId = x.TenantId.ToString(),
                    Code = x.Code,
                    Name = x.Name,
                    SortOrder = x.SortOrder,
                    IsEnabled = x.IsEnabled,
                    CreatedAtUtc = x.CreatedAtUtc,
                    UpdatedAtUtc = x.UpdatedAtUtc,
                }).ToArray(),
            };

            return Results.Ok(ApiResponse<GetCategoriesListResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.SystemPing);

        v1.MapPost("/management/categories", async (
            CreateCategoryRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<CreateCategoryRequest> validator,
            ICategoryManagementService categoryManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var result = await categoryManagementService.CreateAsync(principal, request, cancellationToken);
            if (!result.IsSuccess || result.Payload is null)
            {
                return BuildFailure(
                    context,
                    result.StatusCode ?? StatusCodes.Status400BadRequest,
                    result.ErrorCode ?? "CATEGORY_CREATE_FAILED",
                    result.Message);
            }

            var payload = MapCategory(result.Payload);
            return Results.Ok(ApiResponse<CategoryResponse>.Ok(payload, result.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPatch("/management/categories/{categoryId:guid}", async (
            Guid categoryId,
            UpdateCategoryRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<UpdateCategoryRequest> validator,
            ICategoryManagementService categoryManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var result = await categoryManagementService.UpdateAsync(principal, categoryId, request, cancellationToken);
            if (!result.IsSuccess || result.Payload is null)
            {
                return BuildFailure(
                    context,
                    result.StatusCode ?? StatusCodes.Status400BadRequest,
                    result.ErrorCode ?? "CATEGORY_UPDATE_FAILED",
                    result.Message);
            }

            var payload = MapCategory(result.Payload);
            return Results.Ok(ApiResponse<CategoryResponse>.Ok(payload, result.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPatch("/management/categories/{categoryId:guid}/disable", async (
            Guid categoryId,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            ICategoryManagementService categoryManagementService,
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

            var result = await categoryManagementService.DisableAsync(principal, categoryId, cancellationToken);
            if (!result.IsSuccess || result.Payload is null)
            {
                return BuildFailure(
                    context,
                    result.StatusCode ?? StatusCodes.Status400BadRequest,
                    result.ErrorCode ?? "CATEGORY_DISABLE_FAILED",
                    result.Message);
            }

            var payload = MapCategory(result.Payload);
            return Results.Ok(ApiResponse<CategoryResponse>.Ok(payload, result.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPost("/management/categories/reorder", async (
            ReorderCategoriesRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<ReorderCategoriesRequest> validator,
            ICategoryManagementService categoryManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var result = await categoryManagementService.ReorderAsync(principal, request, cancellationToken);
            if (!result.IsSuccess || result.Payload is null)
            {
                return BuildFailure(
                    context,
                    result.StatusCode ?? StatusCodes.Status400BadRequest,
                    result.ErrorCode ?? "CATEGORY_REORDER_FAILED",
                    result.Message);
            }

            var payload = new GetCategoriesListResponse
            {
                Items = result.Payload.Select(MapCategory).ToArray(),
            };

            return Results.Ok(ApiResponse<GetCategoriesListResponse>.Ok(payload, result.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/workers", async (
            [AsParameters] GetWorkersRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<GetWorkersRequest> validator,
            IWorkerQueryService workerQueryService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var query = await workerQueryService.GetWorkersAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "WORKER_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetWorkersListResponse
            {
                Items = query.Payload.Items.Select(MapWorker).ToArray(),
                Pagination = new GTEK.FSM.Shared.Contracts.Api.Responses.PaginationMetadata
                {
                    Offset = (query.Payload.Page - 1) * query.Payload.PageSize,
                    Limit = query.Payload.PageSize,
                    Total = query.Payload.Total,
                },
            };

            return Results.Ok(ApiResponse<GetWorkersListResponse>.Ok(payload, query.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPost("/management/workers", async (
            CreateWorkerProfileRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<CreateWorkerProfileRequest> validator,
            IWorkerManagementService workerManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var mutation = await workerManagementService.CreateAsync(principal, request, cancellationToken);
            if (!mutation.IsSuccess || mutation.Payload is null)
            {
                return BuildFailure(
                    context,
                    mutation.StatusCode ?? StatusCodes.Status400BadRequest,
                    mutation.ErrorCode ?? "WORKER_CREATE_FAILED",
                    mutation.Message);
            }

            var payload = MapWorker(mutation.Payload);
            return Results.Ok(ApiResponse<WorkerProfileResponse>.Ok(payload, mutation.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPatch("/management/workers/{workerId:guid}", async (
            Guid workerId,
            UpdateWorkerProfileRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<UpdateWorkerProfileRequest> validator,
            IWorkerManagementService workerManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var mutation = await workerManagementService.UpdateAsync(principal, workerId, request, cancellationToken);
            if (!mutation.IsSuccess || mutation.Payload is null)
            {
                return BuildFailure(
                    context,
                    mutation.StatusCode ?? StatusCodes.Status400BadRequest,
                    mutation.ErrorCode ?? "WORKER_UPDATE_FAILED",
                    mutation.Message);
            }

            var payload = MapWorker(mutation.Payload);
            return Results.Ok(ApiResponse<WorkerProfileResponse>.Ok(payload, mutation.Message, context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/subscriptions/organization", async (
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            ISubscriptionQueryService subscriptionQueryService,
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

            var query = await subscriptionQueryService.GetOrganizationAsync(principal, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "SUBSCRIPTION_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetOrganizationSubscriptionResponse
            {
                SubscriptionId = query.Payload.SubscriptionId.ToString(),
                TenantId = query.Payload.TenantId.ToString(),
                PlanCode = query.Payload.PlanCode,
                UserLimit = query.Payload.UserLimit,
                ActiveUsers = query.Payload.ActiveUsers,
                AvailableUserSlots = query.Payload.AvailableUserSlots,
                StartsOnUtc = query.Payload.StartsOnUtc,
                EndsOnUtc = query.Payload.EndsOnUtc,
                RowVersion = query.Payload.RowVersion,
            };

            return Results.Ok(ApiResponse<GetOrganizationSubscriptionResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapPatch("/management/subscriptions/organization", async (
            UpdateOrganizationSubscriptionRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<UpdateOrganizationSubscriptionRequest> validator,
            ISubscriptionManagementService subscriptionManagementService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var update = await subscriptionManagementService.UpdateOrganizationAsync(principal, request, cancellationToken);
            if (!update.IsSuccess || update.Payload is null)
            {
                return BuildFailure(
                    context,
                    update.StatusCode ?? StatusCodes.Status400BadRequest,
                    update.ErrorCode ?? "SUBSCRIPTION_UPDATE_FAILED",
                    update.Message);
            }

            var payload = new GetOrganizationSubscriptionResponse
            {
                SubscriptionId = update.Payload.SubscriptionId.ToString(),
                TenantId = update.Payload.TenantId.ToString(),
                PlanCode = update.Payload.PlanCode,
                UserLimit = update.Payload.UserLimit,
                ActiveUsers = update.Payload.ActiveUsers,
                AvailableUserSlots = update.Payload.AvailableUserSlots,
                StartsOnUtc = update.Payload.StartsOnUtc,
                EndsOnUtc = update.Payload.EndsOnUtc,
                RowVersion = update.Payload.RowVersion,
            };

            return Results.Ok(ApiResponse<GetOrganizationSubscriptionResponse>.Ok(
                data: payload,
                message: update.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/subscriptions/users", async (
            [AsParameters] GetSubscriptionUsersRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<GetSubscriptionUsersRequest> validator,
            ISubscriptionQueryService subscriptionQueryService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var query = await subscriptionQueryService.GetUsersAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "SUBSCRIPTION_USERS_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetSubscriptionUsersListResponse
            {
                Items = query.Payload.Items
                    .Select(x => new GetSubscriptionUserResponse
                    {
                        UserId = x.UserId.ToString(),
                        DisplayName = x.DisplayName,
                        ExternalIdentity = x.ExternalIdentity,
                        IsWithinCurrentPlanLimit = x.IsWithinCurrentPlanLimit,
                    })
                    .ToArray(),
                Pagination = new GTEK.FSM.Shared.Contracts.Api.Responses.PaginationMetadata
                {
                    Offset = (query.Payload.Page - 1) * query.Payload.PageSize,
                    Limit = query.Payload.PageSize,
                    Total = query.Payload.Total,
                },
            };

            return Results.Ok(ApiResponse<GetSubscriptionUsersListResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/audit-logs", async (
            [AsParameters] GetAuditLogsRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<GetAuditLogsRequest> validator,
            IAuditLogQueryService auditLogQueryService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var query = await auditLogQueryService.GetLogsAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "AUDIT_LOG_QUERY_FAILED",
                    query.Message);
            }

            var payload = new GetAuditLogsListResponse
            {
                Items = query.Payload.Items
                    .Select(MapAuditLog)
                    .ToArray(),
                Pagination = new GTEK.FSM.Shared.Contracts.Api.Responses.PaginationMetadata
                {
                    Offset = (query.Payload.Page - 1) * query.Payload.PageSize,
                    Limit = query.Payload.PageSize,
                    Total = query.Payload.Total,
                },
            };

            return Results.Ok(ApiResponse<GetAuditLogsListResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/audit-logs/export", async (
            [AsParameters] GetAuditLogsRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IValidator<GetAuditLogsRequest> validator,
            IAuditLogQueryService auditLogQueryService,
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

            var validationFailure = await BuildValidationFailureAsync(request, validator, context, cancellationToken);
            if (validationFailure is not null)
            {
                return validationFailure;
            }

            var export = await auditLogQueryService.ExportCsvAsync(principal, request, cancellationToken);
            if (!export.IsSuccess || export.Payload is null)
            {
                return BuildFailure(
                    context,
                    export.StatusCode ?? StatusCodes.Status400BadRequest,
                    export.ErrorCode ?? "AUDIT_LOG_EXPORT_FAILED",
                    export.Message);
            }

            var csv = BuildAuditLogCsv(export.Payload);
            var fileName = $"audit-logs-{principal.TenantId:N}-{DateTime.UtcNow:yyyyMMddHHmmss}.csv";
            return Results.File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

        v1.MapGet("/management/reports/overview", async (
            [AsParameters] GetManagementAnalyticsOverviewRequest request,
            HttpContext context,
            IAuthenticatedPrincipalAccessor principalAccessor,
            ITenantContextAccessor tenantContextAccessor,
            IManagementReportingQueryService reportingQueryService,
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

            var query = await reportingQueryService.GetOverviewAsync(principal, request, cancellationToken);
            if (!query.IsSuccess || query.Payload is null)
            {
                return BuildFailure(
                    context,
                    query.StatusCode ?? StatusCodes.Status400BadRequest,
                    query.ErrorCode ?? "REPORTING_QUERY_FAILED",
                    query.Message);
            }

            var payload = MapManagementAnalyticsOverview(query.Payload);
            return Results.Ok(ApiResponse<GetManagementAnalyticsOverviewResponse>.Ok(
                data: payload,
                message: query.Message,
                traceId: context.TraceIdentifier));
        })
        .RequireAuthorization(AuthorizationPolicyCatalog.ManagementFlow);

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

    private static async Task<IResult?> BuildValidationFailureAsync<TRequest>(
        TRequest request,
        IValidator<TRequest> validator,
        HttpContext context,
        CancellationToken cancellationToken)
    {
        ValidationResult validationResult = await validator.ValidateAsync(request, cancellationToken);
        if (validationResult.IsValid)
        {
            return null;
        }

        var message = string.Join(
            "; ",
            validationResult.Errors
                .Select(x => $"{x.PropertyName}: {x.ErrorMessage}")
                .Distinct(StringComparer.Ordinal));

        return BuildFailure(context, StatusCodes.Status400BadRequest, "VALIDATION_FAILED", message);
    }

    private static CategoryResponse MapCategory(QueriedCategoryItem category)
    {
        return new CategoryResponse
        {
            CategoryId = category.CategoryId.ToString(),
            TenantId = category.TenantId.ToString(),
            Code = category.Code,
            Name = category.Name,
            SortOrder = category.SortOrder,
            IsEnabled = category.IsEnabled,
            CreatedAtUtc = category.CreatedAtUtc,
            UpdatedAtUtc = category.UpdatedAtUtc,
        };
    }

    private static WorkerProfileResponse MapWorker(QueriedWorkerProfileItem worker)
    {
        return new WorkerProfileResponse
        {
            WorkerId = worker.WorkerId.ToString(),
            TenantId = worker.TenantId.ToString(),
            WorkerCode = worker.WorkerCode,
            DisplayName = worker.DisplayName,
            InternalRating = worker.InternalRating,
            AvailabilityStatus = worker.AvailabilityStatus.ToString(),
            IsActive = worker.IsActive,
            Skills = worker.Skills.ToArray(),
            CreatedAtUtc = worker.CreatedAtUtc,
            UpdatedAtUtc = worker.UpdatedAtUtc,
        };
    }

    private static GetAuditLogResponse MapAuditLog(QueriedAuditLogItem auditLog)
    {
        return new GetAuditLogResponse
        {
            AuditLogId = auditLog.AuditLogId.ToString(),
            TenantId = auditLog.TenantId.ToString(),
            ActorUserId = auditLog.ActorUserId?.ToString(),
            EntityType = auditLog.EntityType,
            EntityId = auditLog.EntityId.ToString(),
            Action = auditLog.Action,
            Outcome = auditLog.Outcome,
            OccurredAtUtc = auditLog.OccurredAtUtc,
            Details = auditLog.Details,
        };
    }

    private static GetManagementAnalyticsOverviewResponse MapManagementAnalyticsOverview(QueriedManagementAnalyticsOverview overview)
    {
        return new GetManagementAnalyticsOverviewResponse
        {
            TotalRequestsInWindow = overview.TotalRequestsInWindow,
            CompletedRequestsInWindow = overview.CompletedRequestsInWindow,
            ActiveJobs = overview.ActiveJobs,
            SensitiveActions24h = overview.SensitiveActions24h,
            DeniedActions24h = overview.DeniedActions24h,
            IntakeTrend = overview.IntakeTrend
                .Select(x => new ManagementTrendPointResponse
                {
                    DateUtc = x.DateUtc,
                    Value = x.Value,
                })
                .ToArray(),
            CompletionTrend = overview.CompletionTrend
                .Select(x => new ManagementTrendPointResponse
                {
                    DateUtc = x.DateUtc,
                    Value = x.Value,
                })
                .ToArray(),
            Anomalies = overview.Anomalies
                .Select(x => new ManagementAnomalyIndicatorResponse
                {
                    Code = x.Code,
                    Severity = x.Severity,
                    Message = x.Message,
                })
                .ToArray(),
            ActionDrilldown = overview.ActionDrilldown
                .Select(x => new ManagementDrilldownItemResponse
                {
                    Key = x.Key,
                    Count = x.Count,
                })
                .ToArray(),
            OutcomeDrilldown = overview.OutcomeDrilldown
                .Select(x => new ManagementDrilldownItemResponse
                {
                    Key = x.Key,
                    Count = x.Count,
                })
                .ToArray(),
        };
    }

    private static string BuildAuditLogCsv(IReadOnlyList<QueriedAuditLogItem> items)
    {
        var builder = new StringBuilder();
        builder.AppendLine("auditLogId,tenantId,actorUserId,entityType,entityId,action,outcome,occurredAtUtc,details");

        foreach (var item in items)
        {
            builder.Append(EscapeCsv(item.AuditLogId.ToString())).Append(',')
                .Append(EscapeCsv(item.TenantId.ToString())).Append(',')
                .Append(EscapeCsv(item.ActorUserId?.ToString())).Append(',')
                .Append(EscapeCsv(item.EntityType)).Append(',')
                .Append(EscapeCsv(item.EntityId.ToString())).Append(',')
                .Append(EscapeCsv(item.Action)).Append(',')
                .Append(EscapeCsv(item.Outcome)).Append(',')
                .Append(EscapeCsv(item.OccurredAtUtc.ToString("O"))).Append(',')
                .Append(EscapeCsv(item.Details))
                .AppendLine();
        }

        return builder.ToString();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n') && !value.Contains('\r'))
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}
