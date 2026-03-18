namespace GTEK.FSM.Backend.Infrastructure.Configuration;

/// <summary>
/// Placeholder configuration for external service integrations.
/// No external clients are activated in Phase 0.7.5.
/// </summary>
public class ExternalServicesOptions
{
    public NotificationServiceOptions Notifications { get; set; } = new();

    public MapsServiceOptions Maps { get; set; } = new();

    public PaymentServiceOptions Payments { get; set; } = new();

    public WebhookServiceOptions Webhooks { get; set; } = new();
}

public class NotificationServiceOptions
{
    public bool Enabled { get; set; } = false;

    public string Provider { get; set; } = "None";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

public class MapsServiceOptions
{
    public bool Enabled { get; set; } = false;

    public string Provider { get; set; } = "None";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}

public class PaymentServiceOptions
{
    public bool Enabled { get; set; } = false;

    public string Provider { get; set; } = "None";

    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;

    public string WebhookSecret { get; set; } = string.Empty;
}

public class WebhookServiceOptions
{
    public bool Enabled { get; set; } = false;

    public string SignatureHeader { get; set; } = "X-Signature";

    public string SigningSecret { get; set; } = string.Empty;
}
