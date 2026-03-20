using GTEK.FSM.Backend.Api.Routing;
using GTEK.FSM.Backend.Application.Identity;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Architecture;

public class ProtectedEndpointPolicyMetadataTests
{
    [Theory]
    [InlineData("/api/v1/auth/bootstrap/authenticated", AuthorizationPolicyCatalog.SystemPing)]
    [InlineData("/api/v1/auth/bootstrap/forbidden", AuthorizationPolicyCatalog.AdminFlow)]
    [InlineData("/api/v1/tenant/{tenantId:guid}/ownership-check/read", AuthorizationPolicyCatalog.CustomerFlow)]
    [InlineData("/api/v1/tenant/{tenantId:guid}/ownership-check/write", AuthorizationPolicyCatalog.WorkerFlow)]
    [InlineData("/api/v1/management/cross-tenant/{tenantId:guid}/guarded-probe", AuthorizationPolicyCatalog.ManagementFlow)]
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

        builder.Services.AddSingleton<IAuthenticatedPrincipalAccessor, StubPrincipalAccessor>();
        builder.Services.AddSingleton<ITenantContextAccessor, StubTenantContextAccessor>();
        builder.Services.AddSingleton<ITenantOwnershipGuard, StubTenantOwnershipGuard>();
        builder.Services.AddSingleton<IPrivilegedTenantOperationGuard, StubPrivilegedTenantOperationGuard>();

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
