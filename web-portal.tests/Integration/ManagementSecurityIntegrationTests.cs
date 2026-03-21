namespace GTEK.FSM.WebPortal.Tests.Integration;

using Bunit;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Security;
using Microsoft.Extensions.DependencyInjection;

public sealed class ManagementSecurityIntegrationTests : TestContext
{
    public ManagementSecurityIntegrationTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped<UiSecurityContext>();
    }

    [Fact]
    public void ReportsPage_ShowsTenantScopedForbiddenFallback_ForRestrictedReviewActions()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Reports>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            var restrictedHint = cut.Markup.Contains("Forbidden in current tenant/role context.", StringComparison.Ordinal);
            Assert.True(restrictedHint);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void SettingsPage_DisablesGuardrailMutation_ForNonAdminRole()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Settings>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("only admin scope", cut.Markup, StringComparison.OrdinalIgnoreCase);
            var disabledToggles = cut.FindAll("input[type='checkbox'][disabled]");
            Assert.NotEmpty(disabledToggles);
        }, TimeSpan.FromSeconds(3));
    }
}
