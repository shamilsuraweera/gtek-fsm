using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;
using System.Diagnostics;
using System.Diagnostics.Metrics;

using Microsoft.AspNetCore.SignalR;

namespace GTEK.FSM.Backend.Api.Realtime;

public sealed class SignalROperationalUpdatePublisher : IOperationalUpdatePublisher
{
    public const string OperationalUpdateReceivedMethod = "OperationalUpdateReceived";
    private const string ServiceRequestStatusUpdatedEventType = "service_request.status_updated";
    private const string JobAssignmentUpdatedEventType = "job.assignment_updated";
    private const string ServiceRequestSlaEscalatedEventType = "service_request.sla_escalated";

    private static readonly Meter Meter = new("GTEK.FSM.Backend.Api", "1.0.0");
    private static readonly Counter<long> PublishCounter = Meter.CreateCounter<long>(
        name: "realtime_publishes_total",
        unit: "events",
        description: "Total realtime events published.");
    private static readonly Histogram<double> PublishDurationHistogram = Meter.CreateHistogram<double>(
        name: "realtime_publish_duration_ms",
        unit: "ms",
        description: "Realtime publish duration in milliseconds.");

    private readonly IHubContext<OperationsHub> hubContext;
    private readonly ILogger<SignalROperationalUpdatePublisher> logger;

    public SignalROperationalUpdatePublisher(
        IHubContext<OperationsHub> hubContext,
        ILogger<SignalROperationalUpdatePublisher> logger)
    {
        this.hubContext = hubContext;
        this.logger = logger;
    }

    public Task PublishServiceRequestStatusUpdatedAsync(TransitionedServiceRequestPayload payload, CancellationToken cancellationToken = default)
    {
        var envelope = new OperationalUpdateEnvelope
        {
            EventType = ServiceRequestStatusUpdatedEventType,
            TenantId = payload.TenantId.ToString(),
            OccurredAtUtc = payload.UpdatedAtUtc,
            ServiceRequestStatusUpdated = new ServiceRequestStatusUpdatedEvent
            {
                RequestId = payload.RequestId.ToString(),
                TenantId = payload.TenantId.ToString(),
                PreviousStatus = payload.PreviousStatus,
                CurrentStatus = payload.CurrentStatus,
                UpdatedAtUtc = payload.UpdatedAtUtc,
                RowVersion = payload.RowVersion,
            },
        };

        return this.PublishAsync(
            payload.TenantId,
            ServiceRequestStatusUpdatedEventType,
            () => this.hubContext.Clients
            .Groups(
                OperationsHubGroups.ForTenant(payload.TenantId),
                OperationsHubGroups.ForRequest(payload.TenantId, payload.RequestId))
            .SendAsync(OperationalUpdateReceivedMethod, envelope, cancellationToken));
    }

    public Task PublishJobAssignmentUpdatedAsync(AssignedServiceRequestPayload payload, CancellationToken cancellationToken = default)
    {
        var envelope = new OperationalUpdateEnvelope
        {
            EventType = JobAssignmentUpdatedEventType,
            TenantId = payload.TenantId.ToString(),
            OccurredAtUtc = payload.UpdatedAtUtc,
            JobAssignmentUpdated = new JobAssignmentUpdatedEvent
            {
                RequestId = payload.RequestId.ToString(),
                TenantId = payload.TenantId.ToString(),
                JobId = payload.JobId.ToString(),
                PreviousWorkerUserId = payload.PreviousWorkerUserId?.ToString(),
                CurrentWorkerUserId = payload.CurrentWorkerUserId.ToString(),
                AssignmentStatus = payload.AssignmentStatus,
                UpdatedAtUtc = payload.UpdatedAtUtc,
                RowVersion = payload.RowVersion,
            },
        };

        return this.PublishAsync(
            payload.TenantId,
            JobAssignmentUpdatedEventType,
            () => this.hubContext.Clients
            .Groups(
                OperationsHubGroups.ForTenant(payload.TenantId),
                OperationsHubGroups.ForRequest(payload.TenantId, payload.RequestId),
                OperationsHubGroups.ForJob(payload.TenantId, payload.JobId))
            .SendAsync(OperationalUpdateReceivedMethod, envelope, cancellationToken));
    }

    public Task PublishSlaEscalationTriggeredAsync(SlaEscalationTriggeredPayload payload, CancellationToken cancellationToken = default)
    {
        var envelope = new OperationalUpdateEnvelope
        {
            EventType = ServiceRequestSlaEscalatedEventType,
            TenantId = payload.TenantId.ToString(),
            OccurredAtUtc = payload.TriggeredAtUtc,
            ServiceRequestSlaEscalated = new ServiceRequestSlaEscalatedEvent
            {
                RequestId = payload.RequestId.ToString(),
                TenantId = payload.TenantId.ToString(),
                SlaDimension = payload.SlaDimension,
                PreviousSlaStatus = payload.PreviousSlaStatus,
                CurrentSlaStatus = payload.CurrentSlaStatus,
                DueAtUtc = payload.DueAtUtc,
                TriggeredAtUtc = payload.TriggeredAtUtc,
                RowVersion = payload.RowVersion,
            },
        };

        return this.PublishAsync(
            payload.TenantId,
            ServiceRequestSlaEscalatedEventType,
            () => this.hubContext.Clients
            .Groups(
                OperationsHubGroups.ForTenant(payload.TenantId),
                OperationsHubGroups.ForRequest(payload.TenantId, payload.RequestId))
            .SendAsync(OperationalUpdateReceivedMethod, envelope, cancellationToken));
    }

    private async Task PublishAsync(Guid tenantId, string eventType, Func<Task> publishAction)
    {
        var stopwatch = Stopwatch.StartNew();
        var tenantTag = tenantId.ToString();
        var outcome = "success";

        try
        {
            await publishAction();
        }
        catch
        {
            outcome = "failure";
            throw;
        }
        finally
        {
            stopwatch.Stop();

            PublishCounter.Add(1,
                new KeyValuePair<string, object?>("event_type", eventType),
                new KeyValuePair<string, object?>("outcome", outcome),
                new KeyValuePair<string, object?>("tenant", tenantTag));

            PublishDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("event_type", eventType),
                new KeyValuePair<string, object?>("outcome", outcome),
                new KeyValuePair<string, object?>("tenant", tenantTag));

            this.logger.LogInformation(
                "realtime_publish eventType={EventType} tenantId={TenantId} outcome={Outcome} elapsedMs={ElapsedMs}",
                eventType,
                tenantTag,
                outcome,
                stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}