using GTEK.FSM.Backend.Application.ServiceRequests;

namespace GTEK.FSM.Backend.Application.Realtime;

public interface IOperationalUpdatePublisher
{
    Task PublishServiceRequestStatusUpdatedAsync(TransitionedServiceRequestPayload payload, CancellationToken cancellationToken = default);

    Task PublishJobAssignmentUpdatedAsync(AssignedServiceRequestPayload payload, CancellationToken cancellationToken = default);

    Task PublishSlaEscalationTriggeredAsync(SlaEscalationTriggeredPayload payload, CancellationToken cancellationToken = default);
}