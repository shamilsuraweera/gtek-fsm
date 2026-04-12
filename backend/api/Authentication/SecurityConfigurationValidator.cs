namespace GTEK.FSM.Backend.Api.Authentication;

public static class SecurityConfigurationValidator
{
    public static void ValidateForEnvironment(IConfiguration configuration, IHostEnvironment environment)
    {
        var isLocalLike = environment.IsDevelopment() || environment.IsEnvironment("Local");

        ValidateJwt(configuration, allowPlaceholderSecrets: isLocalLike);
        if (isLocalLike)
        {
            return;
        }

        ValidateRequiredSecret(
            GetDatabaseConnectionString(configuration),
            "Database connection string");

        ValidateProviderSecretsWhenEnabled(configuration);
    }

    private static void ValidateJwt(IConfiguration configuration, bool allowPlaceholderSecrets)
    {
        var jwtOptions = new JwtAuthenticationOptions();
        configuration.GetSection(JwtAuthenticationOptions.SectionName).Bind(jwtOptions);
        jwtOptions.Validate(allowPlaceholderSecrets);
    }

    private static string GetDatabaseConnectionString(IConfiguration configuration)
    {
        return configuration["Database:ConnectionString"]
            ?? configuration.GetConnectionString("MainDb")
            ?? string.Empty;
    }

    private static void ValidateProviderSecretsWhenEnabled(IConfiguration configuration)
    {
        var notificationsEnabled = configuration.GetValue<bool>("ExternalServices:Notifications:Enabled");
        if (notificationsEnabled)
        {
            ValidateRequiredSecret(configuration["ExternalServices:Notifications:ApiKey"], "ExternalServices:Notifications:ApiKey");
        }

        var mapsEnabled = configuration.GetValue<bool>("ExternalServices:Maps:Enabled");
        if (mapsEnabled)
        {
            ValidateRequiredSecret(configuration["ExternalServices:Maps:ApiKey"], "ExternalServices:Maps:ApiKey");
        }

        var paymentsEnabled = configuration.GetValue<bool>("ExternalServices:Payments:Enabled");
        if (paymentsEnabled)
        {
            ValidateRequiredSecret(configuration["ExternalServices:Payments:ApiKey"], "ExternalServices:Payments:ApiKey");
            ValidateRequiredSecret(configuration["ExternalServices:Payments:WebhookSecret"], "ExternalServices:Payments:WebhookSecret");
        }

        var storageEnabled = configuration.GetValue<bool>("Features:Storage:Enabled");
        if (!storageEnabled)
        {
            return;
        }

        var defaultProvider = (configuration["Storage:DefaultProvider"] ?? string.Empty).Trim();
        if (defaultProvider.Equals("Blob", StringComparison.OrdinalIgnoreCase))
        {
            ValidateRequiredSecret(configuration["Storage:Blob:ConnectionString"], "Storage:Blob:ConnectionString");
        }
        else if (defaultProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
        {
            ValidateRequiredSecret(configuration["Storage:S3:AccessKey"], "Storage:S3:AccessKey");
            ValidateRequiredSecret(configuration["Storage:S3:SecretKey"], "Storage:S3:SecretKey");
        }
    }

    private static void ValidateRequiredSecret(string? value, string settingName)
    {
        if (string.IsNullOrWhiteSpace(value) || JwtAuthenticationOptions.LooksLikePlaceholderSecret(value))
        {
            throw new InvalidOperationException($"{settingName} must be configured with a non-placeholder secret value outside Development or Local environments.");
        }
    }
}
