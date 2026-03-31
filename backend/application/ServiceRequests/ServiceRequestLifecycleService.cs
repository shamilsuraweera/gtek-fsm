using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Enums;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestLifecycleService : IServiceRequestLifecycleService
{
    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IUnitOfWork unitOfWork;

    public ServiceRequestLifecycleService(
        IServiceRequestRepository serviceRequestRepository,
        IUnitOfWork unitOfWork)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<TransitionServiceRequestResult> TransitionAsync(
        AuthenticatedPrincipal principal,
        Guid requestId,
        string? nextStatus,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nextStatus))
        {
            return TransitionServiceRequestResult.Failure(
                message: "Next status is required.",
                errorCode: "VALIDATION_NEXT_STATUS_REQUIRED",
                statusCode: 400);
        }

        if (!Enum.TryParse<ServiceRequestStatus>(nextStatus.Trim(), ignoreCase: true, out var parsedNextStatus))
        {
            return TransitionServiceRequestResult.Failure(
                message: "Requested status is invalid.",
                errorCode: "VALIDATION_NEXT_STATUS_INVALID",
                statusCode: 400);
        }

        var request = await this.serviceRequestRepository.GetForUpdateAsync(principal.TenantId, requestId, cancellationToken);
        if (request is null)
        {
            return TransitionServiceRequestResult.Failure(
                message: "Service request was not found.",
                errorCode: "REQUEST_NOT_FOUND",
                statusCode: 404);
        }

        var previousStatus = request.Status;

        try
        {
            request.TransitionTo(parsedNextStatus);
        }
        catch (InvalidOperationException ex)
        {
            return TransitionServiceRequestResult.Failure(
                message: ex.Message,
                errorCode: "REQUEST_TRANSITION_INVALID",
                statusCode: 400);
        }

        this.serviceRequestRepository.Update(request);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);

        return TransitionServiceRequestResult.Success(
            new TransitionedServiceRequestPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                PreviousStatus: previousStatus.ToString(),
                CurrentStatus: request.Status.ToString(),
                UpdatedAtUtc: request.UpdatedAtUtc));
    }
}
