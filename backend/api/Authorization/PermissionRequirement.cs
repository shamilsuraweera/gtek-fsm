using Microsoft.AspNetCore.Authorization;

namespace GTEK.FSM.Backend.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
