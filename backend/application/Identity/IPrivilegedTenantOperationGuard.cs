namespace GTEK.FSM.Backend.Application.Identity;

public interface IPrivilegedTenantOperationGuard
{
    Task<PrivilegedTenantOperationGuardResult> EvaluateAsync(
        PrivilegedTenantOperationRequest request,
        CancellationToken cancellationToken = default);
}
