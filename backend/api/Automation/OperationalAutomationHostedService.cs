using System.Diagnostics.Metrics;
using GTEK.FSM.Backend.Application.Automation;
using Microsoft.Extensions.Options;

namespace GTEK.FSM.Backend.Api.Automation;

internal sealed class OperationalAutomationHostedService : BackgroundService
{
    private static readonly Meter Meter = new("GTEK.FSM.Backend.Api.Automation");
    private static readonly Counter<long> RunCounter = Meter.CreateCounter<long>("automation_runs_total");
    private static readonly Counter<long> ActionCounter = Meter.CreateCounter<long>("automation_actions_total");

    private readonly IServiceScopeFactory scopeFactory;
    private readonly IOptionsMonitor<OperationalAutomationSettings> optionsMonitor;
    private readonly ILogger<OperationalAutomationHostedService> logger;

    public OperationalAutomationHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<OperationalAutomationSettings> optionsMonitor,
        ILogger<OperationalAutomationHostedService> logger)
    {
        this.scopeFactory = scopeFactory;
        this.optionsMonitor = optionsMonitor;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var settings = this.optionsMonitor.CurrentValue;

            if (!settings.Enabled)
            {
                await Task.Delay(settings.GetInterval(), stoppingToken);
                continue;
            }

            if (!settings.TryValidate(out var validationError))
            {
                this.logger.LogWarning("automation_runner validation_failed error={ValidationError}", validationError);
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                continue;
            }

            await this.RunOnceAsync(settings, stoppingToken);
            await Task.Delay(settings.GetInterval(), stoppingToken);
        }
    }

    private async Task RunOnceAsync(OperationalAutomationSettings settings, CancellationToken cancellationToken)
    {
        var occurredAtUtc = DateTimeOffset.UtcNow;

        try
        {
            using var scope = this.scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IOperationalAutomationService>();
            var result = await service.ExecuteAsync(settings, occurredAtUtc, cancellationToken);

            RunCounter.Add(1, new KeyValuePair<string, object?>("outcome", "success"));

            foreach (var ruleResult in result.RuleResults)
            {
                if (ruleResult.ExecutedCount > 0)
                {
                    ActionCounter.Add(
                        ruleResult.ExecutedCount,
                        new KeyValuePair<string, object?>("rule", ruleResult.RuleKey),
                        new KeyValuePair<string, object?>("outcome", "executed"));
                }

                if (ruleResult.SkippedCount > 0)
                {
                    ActionCounter.Add(
                        ruleResult.SkippedCount,
                        new KeyValuePair<string, object?>("rule", ruleResult.RuleKey),
                        new KeyValuePair<string, object?>("outcome", "skipped"));
                }
            }

            this.logger.LogInformation(
                "automation_run tenants={TenantCount} executed={ExecutedCount} skipped={SkippedCount}",
                result.TenantCount,
                result.ExecutedCount,
                result.SkippedCount);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            RunCounter.Add(1, new KeyValuePair<string, object?>("outcome", "failure"));
            this.logger.LogError(ex, "automation_run_failed");
        }
    }
}