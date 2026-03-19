namespace GTEK.FSM.Backend.Application.Identity;

public sealed record TokenClaimsPayload(
    Guid UserId,
    Guid TenantId,
    IReadOnlySet<string> Roles,
    int TokenVersion);
