namespace GTEK.FSM.Backend.Application.Automation;

public interface IOperationalAutomationService
{
    Task<OperationalAutomationRunResult> ExecuteAsync(
        OperationalAutomationSettings settings,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken = default);
}