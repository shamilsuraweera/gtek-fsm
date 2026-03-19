namespace GTEK.FSM.Backend.Application.Identity;

public static class TokenClaimsValidator
{
    public static TokenClaimsValidationResult Validate(IReadOnlyDictionary<string, string?> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        var result = new TokenClaimsValidationResult();

        var userId = ParseGuidClaim(claims, TokenClaimNames.Subject, "subject", result);
        var tenantId = ParseGuidClaim(claims, TokenClaimNames.TenantId, "tenant", result);
        var roles = ParseRoles(claims, result);
        var tokenVersion = ParseTokenVersion(claims, result);

        if (result.Issues.Count == 0 && userId.HasValue && tenantId.HasValue && tokenVersion.HasValue)
        {
            result.SetPayload(new TokenClaimsPayload(
                UserId: userId.Value,
                TenantId: tenantId.Value,
                Roles: roles,
                TokenVersion: tokenVersion.Value));
        }

        return result;
    }

    private static Guid? ParseGuidClaim(
        IReadOnlyDictionary<string, string?> claims,
        string claimName,
        string codePrefix,
        TokenClaimsValidationResult result)
    {
        if (!claims.TryGetValue(claimName, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            result.AddIssue(claimName, $"missing_{codePrefix}", $"Required claim '{claimName}' is missing.");
            return null;
        }

        var value = rawValue.Trim();
        if (!Guid.TryParse(value, out var parsed))
        {
            result.AddIssue(claimName, $"malformed_{codePrefix}", $"Claim '{claimName}' must be a valid GUID.");
            return null;
        }

        return parsed;
    }

    private static IReadOnlySet<string> ParseRoles(
        IReadOnlyDictionary<string, string?> claims,
        TokenClaimsValidationResult result)
    {
        claims.TryGetValue(TokenClaimNames.Role, out var roleRaw);
        claims.TryGetValue(TokenClaimNames.Roles, out var rolesRaw);

        if (string.IsNullOrWhiteSpace(roleRaw) && string.IsNullOrWhiteSpace(rolesRaw))
        {
            result.AddIssue(
                TokenClaimNames.Roles,
                "missing_roles",
                $"Required role claim is missing. Provide '{TokenClaimNames.Role}' or '{TokenClaimNames.Roles}'.");
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        var values = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddRoleValues(values, roleRaw);
        AddRoleValues(values, rolesRaw);

        if (values.Count == 0)
        {
            result.AddIssue(
                TokenClaimNames.Roles,
                "malformed_roles",
                $"Role claims '{TokenClaimNames.Role}'/'{TokenClaimNames.Roles}' must contain at least one non-empty role value.");
        }

        return values;
    }

    private static int? ParseTokenVersion(
        IReadOnlyDictionary<string, string?> claims,
        TokenClaimsValidationResult result)
    {
        if (!claims.TryGetValue(TokenClaimNames.TokenVersion, out var rawValue) || string.IsNullOrWhiteSpace(rawValue))
        {
            result.AddIssue(
                TokenClaimNames.TokenVersion,
                "missing_token_version",
                $"Required claim '{TokenClaimNames.TokenVersion}' is missing.");
            return null;
        }

        var value = rawValue.Trim();
        if (!int.TryParse(value, out var version) || version <= 0)
        {
            result.AddIssue(
                TokenClaimNames.TokenVersion,
                "malformed_token_version",
                $"Claim '{TokenClaimNames.TokenVersion}' must be a positive integer.");
            return null;
        }

        return version;
    }

    private static void AddRoleValues(ISet<string> destination, string? rawRoles)
    {
        if (string.IsNullOrWhiteSpace(rawRoles))
        {
            return;
        }

        var fragments = rawRoles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var fragment in fragments)
        {
            if (!string.IsNullOrWhiteSpace(fragment))
            {
                destination.Add(fragment);
            }
        }
    }
}
