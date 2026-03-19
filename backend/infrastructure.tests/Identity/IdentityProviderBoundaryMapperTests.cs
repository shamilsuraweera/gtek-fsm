using GTEK.FSM.Backend.Application.Identity;
using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class IdentityProviderBoundaryMapperTests
{
    [Fact]
    public void ToExternalIdentity_UsesCanonicalProviderSubjectFormat()
    {
        var contract = new IdentityProviderBoundaryContract("AzureAD", "subject-123", "https://issuer.example");

        var externalIdentity = IdentityProviderBoundaryMapper.ToExternalIdentity(contract);

        Assert.Equal("azuread:subject-123", externalIdentity);
    }

    [Fact]
    public void FromExternalIdentity_WithCanonicalFormat_ParsesProviderAndSubject()
    {
        var contract = IdentityProviderBoundaryMapper.FromExternalIdentity("auth0:user-99", "https://issuer.example");

        Assert.Equal("auth0", contract.Provider);
        Assert.Equal("user-99", contract.Subject);
        Assert.Equal("https://issuer.example", contract.Issuer);
    }

    [Fact]
    public void FromExternalIdentity_WithLegacyValue_UsesLegacyProvider()
    {
        var contract = IdentityProviderBoundaryMapper.FromExternalIdentity("legacy-subject-only", "https://issuer.example");

        Assert.Equal(IdentityProviderBoundaryMapper.LegacyProvider, contract.Provider);
        Assert.Equal("legacy-subject-only", contract.Subject);
        Assert.Equal("https://issuer.example", contract.Issuer);
    }
}
