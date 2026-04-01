using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Workers;

public interface IWorkerManagementService
{
    Task<WorkerMutationResult> CreateAsync(
        AuthenticatedPrincipal principal,
        CreateWorkerProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<WorkerMutationResult> UpdateAsync(
        AuthenticatedPrincipal principal,
        Guid workerId,
        UpdateWorkerProfileRequest request,
        CancellationToken cancellationToken = default);
}
