namespace GTEK.FSM.WebPortal.Tests.Component;

using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;
using GTEK.FSM.Shared.Contracts.Vocabulary;
using GTEK.FSM.WebPortal.Services.Security;
using Microsoft.JSInterop;

public sealed class UiSecurityContextAuthorizationUxTests
{
    private readonly UiSecurityContext context = CreateContext();

    [Fact]
    public void GetActionAccessState_ReturnsAllowed_WhenRoleAndTenantMatch()
    {
        var accessState = this.context.GetActionAccessState("TENANT-01", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Allowed, accessState);
    }

    [Fact]
    public void GetActionAccessState_ReturnsHidden_WhenRoleNotAllowed()
    {
        var accessState = this.context.GetActionAccessState("TENANT-01", PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Hidden, accessState);
    }

    [Fact]
    public void GetActionAccessState_ReturnsDisabled_WhenTenantNotAccessible()
    {
        var accessState = this.context.GetActionAccessState("TENANT-99", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxAccessState.Disabled, accessState);
    }

    [Fact]
    public void GetForbiddenFeedback_ReturnsSharedTenantCopy()
    {
        var feedback = this.context.GetForbiddenFeedback("TENANT-99", PortalRole.Support, PortalRole.Manager, PortalRole.Admin);

        Assert.Equal(AuthorizationUxPolicy.ForbiddenByTenantMessage, feedback);
    }

    private static UiSecurityContext CreateContext()
    {
        var authState = new PortalAuthState(new HttpClient(), new TestJsRuntime());
        typeof(PortalAuthState)
            .GetProperty(nameof(PortalAuthState.Session), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(authState, new AuthSessionResponse
            {
                AccessToken = "test-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                UserId = Guid.NewGuid().ToString(),
                TenantId = "TENANT-01",
                TenantCode = "TENANT-01",
                DisplayName = "Support User",
                Email = "support@example.com",
                Role = PortalRole.Support,
            });

        return new UiSecurityContext(authState);
    }

    private sealed class TestJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            return ValueTask.FromResult(default(TValue)!);
        }
    }
}