using GTEK.FSM.Backend.Api.Authentication;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

using Xunit;

namespace GTEK.FSM.Backend.Infrastructure.Tests.Identity;

public class SecurityConfigurationValidatorTests
{
    [Fact]
    public void ValidateForEnvironment_Local_AllowsLocalPlaceholderSigningKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "GTEK.FSM.Local",
            ["Authentication:Jwt:Audience"] = "GTEK.FSM.Clients",
            ["Authentication:Jwt:SigningKey"] = "local-only-signing-key-change-me-32chars-min",
            ["Database:ConnectionString"] = string.Empty,
        });

        var localEnvironment = new StubHostEnvironment("Local");

        var exception = Record.Exception(() => SecurityConfigurationValidator.ValidateForEnvironment(configuration, localEnvironment));

        Assert.Null(exception);
    }

    [Fact]
    public void ValidateForEnvironment_Production_RejectsPlaceholderSigningKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "GTEK.FSM.Prod",
            ["Authentication:Jwt:Audience"] = "GTEK.FSM.Clients",
            ["Authentication:Jwt:SigningKey"] = "CHANGE_ME_WITH_LOCAL_SECRET_MIN_32_CHARS",
            ["Database:ConnectionString"] = "Server=sql-prod;Database=GTEK_FSM;User Id=app;Password=prod-secret;",
        });

        var productionEnvironment = new StubHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => SecurityConfigurationValidator.ValidateForEnvironment(configuration, productionEnvironment));
    }

    [Fact]
    public void ValidateForEnvironment_Production_RejectsMissingDatabaseConnectionString()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "GTEK.FSM.Prod",
            ["Authentication:Jwt:Audience"] = "GTEK.FSM.Clients",
            ["Authentication:Jwt:SigningKey"] = "prod-jwt-signing-key-is-at-least-32-chars",
            ["Database:ConnectionString"] = "",
        });

        var productionEnvironment = new StubHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => SecurityConfigurationValidator.ValidateForEnvironment(configuration, productionEnvironment));
    }

    [Fact]
    public void ValidateForEnvironment_Production_RequiresNotificationApiKeyWhenEnabled()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["Authentication:Jwt:Issuer"] = "GTEK.FSM.Prod",
            ["Authentication:Jwt:Audience"] = "GTEK.FSM.Clients",
            ["Authentication:Jwt:SigningKey"] = "prod-jwt-signing-key-is-at-least-32-chars",
            ["Database:ConnectionString"] = "Server=sql-prod;Database=GTEK_FSM;User Id=app;Password=prod-secret;",
            ["ExternalServices:Notifications:Enabled"] = "true",
            ["ExternalServices:Notifications:ApiKey"] = "",
        });

        var productionEnvironment = new StubHostEnvironment("Production");

        Assert.Throws<InvalidOperationException>(() => SecurityConfigurationValidator.ValidateForEnvironment(configuration, productionEnvironment));
    }

    private static IConfiguration BuildConfiguration(Dictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public StubHostEnvironment(string environmentName)
        {
            this.EnvironmentName = environmentName;
            this.ApplicationName = "GTEK.FSM.Backend.Api.Tests";
            this.ContentRootPath = "/tmp";
            this.ContentRootFileProvider = new NullFileProvider();
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; }

        public string ContentRootPath { get; set; }

        public IFileProvider ContentRootFileProvider { get; set; }
    }
}
