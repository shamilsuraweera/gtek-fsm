using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Workers;

public interface IWorkerQueryService
{
    Task<WorkerProfilesQueryResult> GetWorkersAsync(
        AuthenticatedPrincipal principal,
        GetWorkersRequest request,
        CancellationToken cancellationToken = default);
}
