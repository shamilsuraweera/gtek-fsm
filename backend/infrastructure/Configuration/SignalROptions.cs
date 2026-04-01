namespace GTEK.FSM.Backend.Infrastructure.Configuration;

/// <summary>
/// Configuration for operational SignalR hub behavior.
/// </summary>
public class SignalROptions
{
    public string HubPath { get; set; } = "/hubs/pipeline";

    public bool EnableDetailedErrors { get; set; } = false;

    public int ClientTimeoutSeconds { get; set; } = 30;

    public int HandshakeTimeoutSeconds { get; set; } = 15;
}
