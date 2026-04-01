using GTEK.FSM.Backend.Application.Realtime;
using GTEK.FSM.Backend.Application.ServiceRequests;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Realtime;

using Microsoft.AspNetCore.SignalR;

namespace GTEK.FSM.Backend.Api.Realtime;

public sealed class SignalROperationalUpdatePublisher : IOperationalUpdatePublisher
{
    public const string OperationalUpdateReceivedMethod = "OperationalUpdateReceived";
    private const string ServiceRequestStatusUpdatedEventType = "service_request.status_updated";
    private const string JobAssignmentUpdatedEventType = "job.assignment_updated";

    private readonly IHubContext<OperationsHub> hubContext;

    public SignalROperationalUpdatePublisher(IHubContext<OperationsHub> hubContext)
    {
        this.hubContext = hubContext;
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

        return this.hubContext.Clients
            .Groups(
                OperationsHubGroups.ForTenant(payload.TenantId),
                OperationsHubGroups.ForRequest(payload.TenantId, payload.RequestId))
            .SendAsync(OperationalUpdateReceivedMethod, envelope, cancellationToken);
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

        return this.hubContext.Clients
            .Groups(
                OperationsHubGroups.ForTenant(payload.TenantId),
                OperationsHubGroups.ForRequest(payload.TenantId, payload.RequestId),
                OperationsHubGroups.ForJob(payload.TenantId, payload.JobId))
            .SendAsync(OperationalUpdateReceivedMethod, envelope, cancellationToken);
    }
}