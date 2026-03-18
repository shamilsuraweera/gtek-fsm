namespace GTEK.FSM.Backend.Infrastructure.Configuration;

/// <summary>
/// Placeholder configuration for SignalR-related settings.
/// No SignalR services are activated in Phase 0.7.5.
/// </summary>
public class SignalROptions
{
    public string HubPath { get; set; } = "/hubs/pipeline";

    public bool EnableDetailedErrors { get; set; } = false;

    public int ClientTimeoutSeconds { get; set; } = 30;

    public int HandshakeTimeoutSeconds { get; set; } = 15;
}
