using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Specifications;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Subscriptions;

internal sealed class SubscriptionQueryService : ISubscriptionQueryService
{
    private readonly ISubscriptionRepository subscriptionRepository;
    private readonly IUserRepository userRepository;

    public SubscriptionQueryService(
        ISubscriptionRepository subscriptionRepository,
        IUserRepository userRepository)
    {
        this.subscriptionRepository = subscriptionRepository;
        this.userRepository = userRepository;
    }

    public async Task<OrganizationSubscriptionQueryResult> GetOrganizationAsync(
        AuthenticatedPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "Role is not authorized to access subscription management.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var subscription = await this.subscriptionRepository.GetActiveByTenantAsync(principal.TenantId, cancellationToken);
        if (subscription is null)
        {
            return OrganizationSubscriptionQueryResult.Failure(
                "Active subscription was not found for tenant.",
                "SUBSCRIPTION_NOT_FOUND",
                404);
        }

        var users = await this.userRepository.ListByTenantAsync(principal.TenantId, cancellationToken);
        var activeUsers = users.Count;

        return OrganizationSubscriptionQueryResult.Success(new QueriedOrganizationSubscription(
            SubscriptionId: subscription.Id,
            TenantId: subscription.TenantId,
            PlanCode: subscription.PlanCode,
            UserLimit: subscription.UserLimit,
            ActiveUsers: activeUsers,
            AvailableUserSlots: Math.Max(0, subscription.UserLimit - activeUsers),
            StartsOnUtc: subscription.StartsOnUtc,
            EndsOnUtc: subscription.EndsOnUtc,
            RowVersion: Convert.ToBase64String(subscription.RowVersion)));
    }

    public async Task<SubscriptionUsersQueryResult> GetUsersAsync(
        AuthenticatedPrincipal principal,
        GetSubscriptionUsersRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsManagementRole(principal))
        {
            return SubscriptionUsersQueryResult.Failure(
                "Role is not authorized to access subscription management.",
                "AUTH_FORBIDDEN_ROLE",
                403);
        }

        var subscription = await this.subscriptionRepository.GetActiveByTenantAsync(principal.TenantId, cancellationToken);
        if (subscription is null)
        {
            return SubscriptionUsersQueryResult.Failure(
                "Active subscription was not found for tenant.",
                "SUBSCRIPTION_NOT_FOUND",
                404);
        }

        var page = new PageSpecification(request.Page ?? 1, request.PageSize ?? 25);
        var users = await this.userRepository.ListByTenantAsync(principal.TenantId, cancellationToken);

        var filtered = users
            .Where(x => MatchesSearch(x.DisplayName, x.ExternalIdentity, request.SearchText))
            .ToList();

        var paged = filtered
            .Skip(page.Skip)
            .Take(page.Take)
            .Select((x, index) => new QueriedSubscriptionUserItem(
                UserId: x.Id,
                DisplayName: x.DisplayName,
                ExternalIdentity: x.ExternalIdentity,
                IsWithinCurrentPlanLimit: page.Skip + index < subscription.UserLimit))
            .ToArray();

        return SubscriptionUsersQueryResult.Success(
            new QueriedSubscriptionUsersPage(
                Items: paged,
                Page: page.NormalizedPageNumber,
                PageSize: page.NormalizedPageSize,
                Total: filtered.Count));
    }

    private static bool IsManagementRole(AuthenticatedPrincipal principal)
    {
        return principal.IsInRole("Manager") || principal.IsInRole("Admin");
    }

    private static bool MatchesSearch(string displayName, string externalIdentity, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        var term = searchText.Trim();
        return displayName.Contains(term, StringComparison.OrdinalIgnoreCase)
            || externalIdentity.Contains(term, StringComparison.OrdinalIgnoreCase);
    }
}