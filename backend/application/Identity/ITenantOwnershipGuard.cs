namespace GTEK.FSM.Backend.Application.Identity;

public interface ITenantOwnershipGuard
{
    TenantOwnershipGuardResult EnsureTenantAccess(Guid requestedTenantId);
}
