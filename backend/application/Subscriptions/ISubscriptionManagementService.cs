using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Subscriptions;

public interface ISubscriptionManagementService
{
    Task<OrganizationSubscriptionQueryResult> UpdateOrganizationAsync(
        AuthenticatedPrincipal principal,
        UpdateOrganizationSubscriptionRequest request,
        CancellationToken cancellationToken = default);
}