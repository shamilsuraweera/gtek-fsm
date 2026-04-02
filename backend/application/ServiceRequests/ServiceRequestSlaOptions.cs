namespace GTEK.FSM.Backend.Application.ServiceRequests;

public sealed class ServiceRequestSlaOptions
{
    public int ResponseMinutes { get; set; } = 15;

    public int AssignmentMinutes { get; set; } = 30;

    public int CompletionMinutes { get; set; } = 240;

    // Percentage threshold where an SLA transitions from OnTrack to AtRisk.
    public decimal AtRiskThresholdPercent { get; set; } = 80m;
}
