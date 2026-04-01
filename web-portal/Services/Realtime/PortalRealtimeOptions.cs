namespace GTEK.FSM.WebPortal.Services.Realtime;

public sealed class PortalRealtimeOptions
{
    public const string SectionName = "PortalRealtime";

    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = string.Empty;

    public string HubPath { get; set; } = "/hubs/pipeline";
}