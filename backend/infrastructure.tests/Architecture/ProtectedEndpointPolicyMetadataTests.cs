using GTEK.FSM.Backend.Api.Authentication;
using GTEK.FSM.Backend.Api.Authorization;
using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Application;
using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Architecture;

public class ProtectedEndpointPolicyMetadataTests
{
    [Theory]
    // Auth probes
    [InlineData("/api/v1/auth/bootstrap/authenticated", AuthorizationPolicyCatalog.SystemPing)]
    [InlineData("/api/v1/auth/bootstrap/forbidden", AuthorizationPolicyCatalog.AdminFlow)]
    [InlineData("/api/v1/tenant/{tenantId:guid}/ownership-check/read", AuthorizationPolicyCatalog.CustomerFlow)]
    [InlineData("/api/v1/tenant/{tenantId:guid}/ownership-check/write", AuthorizationPolicyCatalog.WorkerFlow)]
    [InlineData("/api/v1/management/cross-tenant/{tenantId:guid}/guarded-probe", AuthorizationPolicyCatalog.ManagementFlow)]
    // Customer / request flows
    [InlineData("/api/v1/requests", AuthorizationPolicyCatalog.CustomerFlow)]
    [InlineData("/api/v1/requests/{requestId:guid}", AuthorizationPolicyCatalog.SystemPing)]
    // Support / dispatch flows
    [InlineData("/api/v1/requests/{requestId:guid}/assign", AuthorizationPolicyCatalog.SupportFlow)]
    [InlineData("/api/v1/requests/{requestId:guid}/reassign", AuthorizationPolicyCatalog.SupportFlow)]
    // Worker flows
    [InlineData("/api/v1/jobs", AuthorizationPolicyCatalog.SystemPing)]
    [InlineData("/api/v1/jobs/{jobId:guid}", AuthorizationPolicyCatalog.SystemPing)]
    [InlineData("/api/v1/workers/candidates", AuthorizationPolicyCatalog.SupportFlow)]
    // Management flows
    [InlineData("/api/v1/management/workers", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/categories", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/subscriptions/organization", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/subscriptions/users", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/audit-logs", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/audit-logs/export", AuthorizationPolicyCatalog.ManagementFlow)]
    [InlineData("/api/v1/management/reports/overview", AuthorizationPolicyCatalog.ManagementFlow)]
    public void MapV1Endpoints_ProtectedRoutes_RequireExpectedPolicy(string routePattern, string expectedPolicy)
    {
        var app = BuildApp();

        app.MapV1Endpoints();

        var endpoint = ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(x => x.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(x => IsSameRoutePattern(x.RoutePattern.RawText, routePattern));

        var authorizeMetadata = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>();

        Assert.NotEmpty(authorizeMetadata);
        Assert.Contains(authorizeMetadata, x => string.Equals(x.Policy, expectedPolicy, StringComparison.Ordinal));
    }

    private static WebApplication BuildApp()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddApplication();
        builder.Services.AddSingleton<IAuthenticatedPrincipalAccessor, StubPrincipalAccessor>();
        builder.Services.AddSingleton<ITenantContextAccessor, StubTenantContextAccessor>();
        builder.Services.AddSingleton<ITenantOwnershipGuard, StubTenantOwnershipGuard>();
        builder.Services.AddSingleton<IPrivilegedTenantOperationGuard, StubPrivilegedTenantOperationGuard>();
        builder.Services.AddSingleton<ILocalAuthService, StubLocalAuthService>();
        builder.Services.AddApiAuthorizationPolicies();

        return builder.Build();
    }

    private sealed class StubPrincipalAccessor : IAuthenticatedPrincipalAccessor
    {
        public AuthenticatedPrincipal? GetCurrent() => null;
    }

    private sealed class StubTenantContextAccessor : ITenantContextAccessor
    {
        public Guid? GetCurrentTenantId() => null;
    }

    private sealed class StubTenantOwnershipGuard : ITenantOwnershipGuard
    {
        public TenantOwnershipGuardResult EnsureTenantAccess(Guid requestedTenantId) => TenantOwnershipGuardResult.Allow();
    }

    private sealed class StubPrivilegedTenantOperationGuard : IPrivilegedTenantOperationGuard
    {
        public Task<PrivilegedTenantOperationGuardResult> EvaluateAsync(
            PrivilegedTenantOperationRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(PrivilegedTenantOperationGuardResult.Allow());
        }
    }

    private sealed class StubLocalAuthService : ILocalAuthService
    {
        public Task<LocalAuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));

        public Task<LocalAuthResult> RegisterAsync(RegisterLocalUserRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(LocalAuthResult.Fail(401, "STUB", "stub"));
    }

    private static bool IsSameRoutePattern(string? actual, string expected)
    {
        var normalizedActual = Normalize(actual);
        var normalizedExpected = Normalize(expected);

        return string.Equals(normalizedActual, normalizedExpected, StringComparison.Ordinal)
            || normalizedActual.EndsWith(normalizedExpected, StringComparison.Ordinal);
    }

    private static string Normalize(string? routePattern)
    {
        return (routePattern ?? string.Empty).Trim().TrimStart('/');
    }
}
