using System.Security.Claims;
using GTEK.FSM.Backend.Application.Identity;
using Microsoft.AspNetCore.Http;

namespace GTEK.FSM.Backend.Infrastructure.Identity;

internal sealed class HttpContextAuthenticatedPrincipalAccessor : IAuthenticatedPrincipalAccessor
{
    private readonly IHttpContextAccessor httpContextAccessor;

    public HttpContextAuthenticatedPrincipalAccessor(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }

    public AuthenticatedPrincipal? GetCurrent()
    {
        var principal = this.httpContextAccessor.HttpContext?.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        var claims = BuildClaimsDictionary(principal);
        var validation = TokenClaimsValidator.Validate(claims);
        if (!validation.IsValid || validation.Payload is null)
        {
            return null;
        }

        return new AuthenticatedPrincipal(
            validation.Payload.UserId,
            validation.Payload.TenantId,
            roles: validation.Payload.Roles,
            scopes: Array.Empty<string>());
    }

    private static IReadOnlyDictionary<string, string?> BuildClaimsDictionary(ClaimsPrincipal principal)
    {
        var claims = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            [TokenClaimNames.Subject] = principal.FindFirstValue(TokenClaimNames.Subject),
            [TokenClaimNames.TenantId] = principal.FindFirstValue(TokenClaimNames.TenantId),
            [TokenClaimNames.TokenVersion] = principal.FindFirstValue(TokenClaimNames.TokenVersion),
        };

        var roleValues = principal.FindAll(TokenClaimNames.Role)
            .Select(x => x.Value?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        var rolesValues = principal.FindAll(TokenClaimNames.Roles)
            .Select(x => x.Value?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();

        claims[TokenClaimNames.Role] = roleValues.Length > 0 ? string.Join(',', roleValues!) : null;
        claims[TokenClaimNames.Roles] = rolesValues.Length > 0 ? string.Join(',', rolesValues!) : null;

        return claims;
    }
}
