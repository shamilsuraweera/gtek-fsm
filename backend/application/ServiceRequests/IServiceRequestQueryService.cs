//-----------------------------------------------------------------------
// <copyright file="IServiceRequestQueryService.cs" company="GTEK">
// Copyright (c) 2026 GTEK. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Defines request query operations for tenant-scoped service request listings.
/// </summary>
public interface IServiceRequestQueryService
{
    /// <summary>
    /// Retrieves service requests for the authenticated principal using the supplied query parameters.
    /// </summary>
    /// <param name="principal">The authenticated principal used for tenant and role scoping.</param>
    /// <param name="request">The incoming request query contract.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A result envelope containing either query results or failure details.</returns>
    Task<ServiceRequestQueryResult> QueryAsync(
        AuthenticatedPrincipal principal,
        GetRequestsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves service request detail for the authenticated principal.
    /// </summary>
    /// <param name="principal">The authenticated principal used for tenant and role scoping.</param>
    /// <param name="requestId">The target service request id.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A result envelope containing detail payload or failure details.</returns>
    Task<ServiceRequestDetailQueryResult> GetDetailAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        CancellationToken cancellationToken = default);
}
