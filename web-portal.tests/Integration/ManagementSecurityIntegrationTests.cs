namespace GTEK.FSM.WebPortal.Tests.Integration;

using Bunit;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Management;
using GTEK.FSM.WebPortal.Services.Security;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
using Microsoft.Extensions.DependencyInjection;

public sealed class ManagementSecurityIntegrationTests : TestContext
{
    public ManagementSecurityIntegrationTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped<UiSecurityContext>();
        this.Services.AddScoped<IManagementWorkersApiClient>(_ => new FakeManagementWorkersApiClient());
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

    [Fact]
    public void WorkersPage_LoadsAndRendersWorkerRows_FromManagementApiClient()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Workers>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Worker Profiles", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("WRK-OPS-01", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Ops Worker", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    private sealed class FakeManagementWorkersApiClient : IManagementWorkersApiClient
    {
        private readonly List<WorkerProfileResponse> workers =
        [
            new WorkerProfileResponse
            {
                WorkerId = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                WorkerCode = "WRK-OPS-01",
                DisplayName = "Ops Worker",
                InternalRating = 4.4m,
                AvailabilityStatus = "Available",
                IsActive = true,
                Skills = ["DISPATCH", "HVAC"],
                CreatedAtUtc = DateTime.UtcNow.AddDays(-7),
                UpdatedAtUtc = DateTime.UtcNow,
            },
        ];

        public Task<IReadOnlyList<WorkerProfileResponse>> ListAsync(string? searchText, bool includeInactive, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkerProfileResponse>>(this.workers);
        }

        public Task<WorkerProfileResponse> CreateAsync(CreateWorkerProfileRequest request, CancellationToken cancellationToken = default)
        {
            var worker = new WorkerProfileResponse
            {
                WorkerId = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                WorkerCode = request.WorkerCode,
                DisplayName = request.DisplayName,
                InternalRating = request.InternalRating ?? 3.0m,
                AvailabilityStatus = request.AvailabilityStatus ?? "Available",
                IsActive = request.IsActive ?? true,
                Skills = request.Skills ?? [],
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            this.workers.Add(worker);
            return Task.FromResult(worker);
        }

        public Task<WorkerProfileResponse> UpdateAsync(string workerId, UpdateWorkerProfileRequest request, CancellationToken cancellationToken = default)
        {
            var worker = this.workers.First(x => string.Equals(x.WorkerId, workerId, StringComparison.Ordinal));
            worker.WorkerCode = request.WorkerCode ?? worker.WorkerCode;
            worker.DisplayName = request.DisplayName ?? worker.DisplayName;
            worker.InternalRating = request.InternalRating ?? worker.InternalRating;
            worker.AvailabilityStatus = request.AvailabilityStatus ?? worker.AvailabilityStatus;
            worker.IsActive = request.IsActive ?? worker.IsActive;
            if (request.Skills is not null)
            {
                worker.Skills = request.Skills;
            }

            worker.UpdatedAtUtc = DateTime.UtcNow;
            return Task.FromResult(worker);
        }
    }
}
