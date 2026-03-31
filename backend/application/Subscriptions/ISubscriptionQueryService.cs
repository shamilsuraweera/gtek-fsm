using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Subscriptions;

public interface ISubscriptionQueryService
{
    Task<OrganizationSubscriptionQueryResult> GetOrganizationAsync(
        AuthenticatedPrincipal principal,
        CancellationToken cancellationToken = default);

    Task<SubscriptionUsersQueryResult> GetUsersAsync(
        AuthenticatedPrincipal principal,
        GetSubscriptionUsersRequest request,
        CancellationToken cancellationToken = default);
}