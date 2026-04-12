namespace GTEK.FSM.WebPortal.Tests.Integration;

using Bunit;
using System.Reflection;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Responses;
using GTEK.FSM.WebPortal.Pages.Management;
using GTEK.FSM.WebPortal.Services;
using GTEK.FSM.WebPortal.Services.Management;
using GTEK.FSM.WebPortal.Services.Security;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Responses;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Reports.Responses;
using GTEK.FSM.Shared.Contracts.Api.Responses;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

public sealed class ManagementSecurityIntegrationTests : TestContext
{
    public ManagementSecurityIntegrationTests()
    {
        this.Services.AddScoped<ResilientDataFetcher>();
        this.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri("http://localhost/") });
        this.Services.AddScoped<PortalAuthState>(sp => BuildPortalAuthState(sp.GetRequiredService<IJSRuntime>()));
        this.Services.AddScoped<UiSecurityContext>();
        this.Services.AddScoped<IManagementWorkersApiClient>(_ => new FakeManagementWorkersApiClient());
        this.Services.AddScoped<IManagementSubscriptionsApiClient>(_ => new FakeManagementSubscriptionsApiClient());
        this.Services.AddScoped<IManagementCategoriesApiClient>(_ => new FakeManagementCategoriesApiClient());
        this.Services.AddScoped<IManagementReportsApiClient>(_ => new FakeManagementReportsApiClient());
    }

    [Fact]
    public void ReportsPage_ShowsTenantScopedForbiddenFallback_ForRestrictedReviewActions()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Reports>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Anomaly Indicators", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("DENIED_ACTION_SPIKE", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Continuous Improvement Cadence", cut.Markup, StringComparison.Ordinal);
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

    [Fact]
    public void SubscriptionsPage_LoadsAndRendersOrgSubscription_FromManagementApiClient()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Subscriptions>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Organisation Subscription", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("PRO", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("50", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void CategoriesPage_LoadsAndRendersCategoryRows_FromManagementApiClient()
    {
        // Arrange + Act
        var cut = this.RenderComponent<Categories>();

        // Assert
        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Category Catalog", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("ELEC", cut.Markup, StringComparison.Ordinal);
            Assert.Contains("Electrical", cut.Markup, StringComparison.Ordinal);
        }, TimeSpan.FromSeconds(3));
    }

    private static PortalAuthState BuildPortalAuthState(IJSRuntime jsRuntime)
    {
        var authState = new PortalAuthState(new HttpClient { BaseAddress = new Uri("http://localhost/") }, jsRuntime);

        typeof(PortalAuthState)
            .GetProperty(nameof(PortalAuthState.Session), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(authState, new AuthSessionResponse
            {
                AccessToken = "test-token",
                ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1),
                UserId = "manager-01",
                TenantId = "TENANT-01",
                TenantCode = "tenant-01",
                DisplayName = "Manager Operator",
                Email = "manager@example.com",
                Role = PortalRole.Manager,
            });

        typeof(PortalAuthState)
            .GetProperty(nameof(PortalAuthState.IsInitialized), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?.SetValue(authState, true);

        return authState;
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

    private sealed class FakeManagementSubscriptionsApiClient : IManagementSubscriptionsApiClient
    {
        private readonly GetOrganizationSubscriptionResponse org = new()
        {
            SubscriptionId = Guid.NewGuid().ToString(),
            TenantId = Guid.NewGuid().ToString(),
            PlanCode = "PRO",
            UserLimit = 50,
            ActiveUsers = 12,
            AvailableUserSlots = 38,
            StartsOnUtc = DateTime.UtcNow.AddYears(-1),
            EndsOnUtc = DateTime.UtcNow.AddYears(1),
            RowVersion = Convert.ToBase64String(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }),
        };

        public Task<GetOrganizationSubscriptionResponse> GetOrganizationAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(this.org);
        }

        public Task<GetOrganizationSubscriptionResponse> UpdateOrganizationAsync(UpdateOrganizationSubscriptionRequest request, CancellationToken cancellationToken = default)
        {
            this.org.PlanCode = request.PlanCode ?? this.org.PlanCode;
            this.org.UserLimit = request.UserLimit ?? this.org.UserLimit;
            return Task.FromResult(this.org);
        }

        public Task<GetSubscriptionUsersListResponse> GetUsersAsync(string? searchText, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetSubscriptionUsersListResponse
            {
                Items = Array.Empty<GetSubscriptionUserResponse>(),
                Pagination = new PaginationMetadata { Total = 0, Limit = pageSize, Offset = 0 },
            });
        }
    }

    private sealed class FakeManagementReportsApiClient : IManagementReportsApiClient
    {
        public Task<GetManagementAnalyticsOverviewResponse> GetOverviewAsync(int? windowDays = null, int? trendBuckets = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new GetManagementAnalyticsOverviewResponse
            {
                TotalRequestsInWindow = 22,
                CompletedRequestsInWindow = 11,
                ActiveJobs = 5,
                SensitiveActions24h = 9,
                DeniedActions24h = 3,
                IntakeTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.Date.AddDays(-1), Value = 3 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.Date, Value = 4 },
                ],
                CompletionTrend =
                [
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.Date.AddDays(-1), Value = 2 },
                    new ManagementTrendPointResponse { DateUtc = DateTime.UtcNow.Date, Value = 1 },
                ],
                Anomalies =
                [
                    new ManagementAnomalyIndicatorResponse
                    {
                        Code = "DENIED_ACTION_SPIKE",
                        Severity = "High",
                        Message = "Denied actions increased.",
                    },
                ],
                ActionDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "CATEGORY_UPDATED", Count = 6 },
                ],
                OutcomeDrilldown =
                [
                    new ManagementDrilldownItemResponse { Key = "Success", Count = 6 },
                ],
                ContinuousImprovement = new ManagementContinuousImprovementResponse
                {
                    CadenceName = "Weekly KPI Review",
                    ReviewWindowDays = 7,
                    NextReviewOnUtc = DateTime.UtcNow.AddDays(7),
                    PrioritizationRule = "High items become immediate backlog candidates.",
                    ImprovementItems =
                    [
                        new ManagementImprovementItemResponse
                        {
                            Code = "ASSIGNMENT_ACCEPTANCE_TUNING",
                            Priority = "High",
                            Metric = "Assignment acceptance rate",
                            CurrentState = "66.67% across active assignment events.",
                            TargetState = "Maintain at least 80% assignment acceptance.",
                            RecommendedAction = "Review rejected and pending assignment bands.",
                            ReviewOwner = "Dispatch Lead",
                        },
                    ],
                },
            });
        }
    }

    private sealed class FakeManagementCategoriesApiClient : IManagementCategoriesApiClient
    {
        private readonly List<CategoryResponse> categories =
        [
            new CategoryResponse
            {
                CategoryId = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                Code = "ELEC",
                Name = "Electrical",
                SortOrder = 0,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-20),
                UpdatedAtUtc = DateTime.UtcNow,
            },
            new CategoryResponse
            {
                CategoryId = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                Code = "HVAC",
                Name = "HVAC",
                SortOrder = 1,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow.AddDays(-12),
                UpdatedAtUtc = DateTime.UtcNow,
            },
        ];

        public Task<IReadOnlyList<CategoryResponse>> ListAsync(bool includeDisabled, CancellationToken cancellationToken = default)
        {
            var result = includeDisabled
                ? this.categories.ToList()
                : this.categories.Where(x => x.IsEnabled).ToList();
            return Task.FromResult<IReadOnlyList<CategoryResponse>>(result);
        }

        public Task<CategoryResponse> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var item = new CategoryResponse
            {
                CategoryId = Guid.NewGuid().ToString(),
                TenantId = Guid.NewGuid().ToString(),
                Code = request.Code,
                Name = request.Name,
                SortOrder = request.SortOrder ?? this.categories.Count,
                IsEnabled = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
            };

            this.categories.Add(item);
            return Task.FromResult(item);
        }

        public Task<CategoryResponse> UpdateAsync(string categoryId, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
        {
            var item = this.categories.First(x => string.Equals(x.CategoryId, categoryId, StringComparison.Ordinal));
            item.Code = request.Code ?? item.Code;
            item.Name = request.Name ?? item.Name;
            item.SortOrder = request.SortOrder ?? item.SortOrder;
            item.IsEnabled = request.IsEnabled ?? item.IsEnabled;
            item.UpdatedAtUtc = DateTime.UtcNow;
            return Task.FromResult(item);
        }

        public Task<CategoryResponse> DisableAsync(string categoryId, CancellationToken cancellationToken = default)
        {
            var item = this.categories.First(x => string.Equals(x.CategoryId, categoryId, StringComparison.Ordinal));
            item.IsEnabled = false;
            item.UpdatedAtUtc = DateTime.UtcNow;
            return Task.FromResult(item);
        }

        public Task<IReadOnlyList<CategoryResponse>> ReorderAsync(ReorderCategoriesRequest request, CancellationToken cancellationToken = default)
        {
            foreach (var reorderItem in request.Items)
            {
                var item = this.categories.FirstOrDefault(x => string.Equals(x.CategoryId, reorderItem.CategoryId, StringComparison.Ordinal));
                if (item is not null)
                {
                    item.SortOrder = reorderItem.SortOrder ?? item.SortOrder;
                }
            }

            var ordered = this.categories.OrderBy(x => x.SortOrder).ToList();
            return Task.FromResult<IReadOnlyList<CategoryResponse>>(ordered);
        }
    }
}
