using GTEK.FSM.Backend.Application.Identity;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class TokenClaimsValidatorTests
{
    [Fact]
    public void Validate_WithValidClaims_ReturnsPayload()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();

        var claims = new Dictionary<string, string?>
        {
            [TokenClaimNames.Subject] = userId.ToString(),
            [TokenClaimNames.TenantId] = tenantId.ToString(),
            [TokenClaimNames.Roles] = "Support, Manager",
            [TokenClaimNames.TokenVersion] = "1",
        };

        var result = TokenClaimsValidator.Validate(claims);

        Assert.True(result.IsValid);
        Assert.NotNull(result.Payload);
        Assert.Equal(userId, result.Payload!.UserId);
        Assert.Equal(tenantId, result.Payload.TenantId);
        Assert.Contains("Support", result.Payload.Roles);
        Assert.Contains("Manager", result.Payload.Roles);
        Assert.Equal(1, result.Payload.TokenVersion);
    }

    [Fact]
    public void Validate_MissingSubject_ReturnsMissingSubjectIssue()
    {
        var claims = BaselineClaims();
        claims.Remove(TokenClaimNames.Subject);

        var result = TokenClaimsValidator.Validate(claims);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "missing_subject");
    }

    [Fact]
    public void Validate_MalformedTenantId_ReturnsMalformedTenantIssue()
    {
        var claims = BaselineClaims();
        claims[TokenClaimNames.TenantId] = "not-a-guid";

        var result = TokenClaimsValidator.Validate(claims);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "malformed_tenant");
    }

    [Fact]
    public void Validate_MissingRoles_ReturnsMissingRolesIssue()
    {
        var claims = BaselineClaims();
        claims.Remove(TokenClaimNames.Role);
        claims.Remove(TokenClaimNames.Roles);

        var result = TokenClaimsValidator.Validate(claims);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "missing_roles");
    }

    [Fact]
    public void Validate_MalformedTokenVersion_ReturnsMalformedTokenVersionIssue()
    {
        var claims = BaselineClaims();
        claims[TokenClaimNames.TokenVersion] = "0";

        var result = TokenClaimsValidator.Validate(claims);

        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, x => x.Code == "malformed_token_version");
    }

    private static Dictionary<string, string?> BaselineClaims()
    {
        return new Dictionary<string, string?>
        {
            [TokenClaimNames.Subject] = Guid.NewGuid().ToString(),
            [TokenClaimNames.TenantId] = Guid.NewGuid().ToString(),
            [TokenClaimNames.Role] = "Support",
            [TokenClaimNames.TokenVersion] = "1",
        };
    }
}
