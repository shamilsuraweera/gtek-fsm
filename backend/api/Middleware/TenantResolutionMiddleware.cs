using System.Security.Claims;

using GTEK.FSM.Backend.Api.Tenancy;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Results;

using Microsoft.Extensions.Options;

namespace GTEK.FSM.Backend.Api.Middleware;

public sealed class TenantResolutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly TenantResolutionOptions _options;

    public TenantResolutionMiddleware(RequestDelegate next, IOptions<TenantResolutionOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.RequireTenantResolution)
        {
            await _next(context);
            return;
        }

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var claimTenant = context.User.FindFirstValue(TokenClaimNames.TenantId);
        var headerTenant = context.Request.Headers.TryGetValue(_options.HeaderName, out var values)
            ? values.ToString()
            : null;

        var resolution = TenantResolutionPolicy.Resolve(new TenantResolutionInput(
            TenantClaimValue: claimTenant,
            TenantHeaderValue: headerTenant,
            AllowHeaderFallback: CanUseHeaderFallback(context.User, _options.HeaderFallbackAllowedRoles)));

        if (!resolution.IsSuccess)
        {
            await WriteRejectResponse(context, resolution);
            return;
        }

        context.Items[TenantContextConstants.HttpContextItemKey] = resolution.TenantId!.Value;
        await _next(context);
    }

    private static bool CanUseHeaderFallback(ClaimsPrincipal principal, IReadOnlyCollection<string> allowedRoles)
    {
        if (allowedRoles.Count == 0)
        {
            return false;
        }

        foreach (var role in allowedRoles)
        {
            if (principal.IsInRole(role))
            {
                return true;
            }
        }

        var claimRoles = principal.Claims
            .Where(c => c.Type is ClaimTypes.Role or TokenClaimNames.Role or TokenClaimNames.Roles)
            .SelectMany(c => c.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return allowedRoles.Any(claimRoles.Contains);
    }

    private static async Task WriteRejectResponse(HttpContext context, TenantResolutionOutcome outcome)
    {
        context.Response.StatusCode = outcome.StatusCode ?? StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object>.Fail(
            message: outcome.Message ?? "Tenant resolution failed.",
            errorCode: outcome.ErrorCode ?? "TENANT_CONTEXT_UNRESOLVED");

        await context.Response.WriteAsJsonAsync(payload);
    }
}
