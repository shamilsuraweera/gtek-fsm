namespace GTEK.FSM.Backend.Application.Identity;

public interface ITenantContextAccessor
{
    Guid? GetCurrentTenantId();
}
