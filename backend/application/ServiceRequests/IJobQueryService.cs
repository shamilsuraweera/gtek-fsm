using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

/// <summary>
/// Defines job query operations for tenant-scoped listings.
/// </summary>
public interface IJobQueryService
{
    /// <summary>
    /// Retrieves jobs for the authenticated principal using supplied query parameters.
    /// </summary>
    /// <param name="principal">The authenticated principal used for tenant and role scoping.</param>
    /// <param name="request">The incoming jobs query contract.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A result envelope containing either query results or failure details.</returns>
    Task<JobQueryResult> QueryAsync(
        AuthenticatedPrincipal principal,
        GetJobsRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves job detail for the authenticated principal.
    /// </summary>
    /// <param name="principal">The authenticated principal used for tenant and role scoping.</param>
    /// <param name="jobId">The target job id.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>A result envelope containing detail payload or failure details.</returns>
    Task<JobDetailQueryResult> GetDetailAsync(
        AuthenticatedPrincipal principal,
        Guid jobId,
        CancellationToken cancellationToken = default);
}
