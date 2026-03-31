using GTEK.FSM.Backend.Application.Identity;
using GTEK.FSM.Backend.Application.Persistence.Repositories;
using GTEK.FSM.Backend.Application.Persistence.Transactions;
using GTEK.FSM.Backend.Domain.Aggregates;

namespace GTEK.FSM.Backend.Application.ServiceRequests;

internal sealed class ServiceRequestCreationService : IServiceRequestCreationService
{
    private const int MaxTitleLength = 180;

    private readonly IServiceRequestRepository serviceRequestRepository;
    private readonly IUnitOfWork unitOfWork;

    public ServiceRequestCreationService(
        IServiceRequestRepository serviceRequestRepository,
        IUnitOfWork unitOfWork)
    {
        this.serviceRequestRepository = serviceRequestRepository;
        this.unitOfWork = unitOfWork;
    }

    public async Task<CreateServiceRequestResult> CreateAsync(AuthenticatedPrincipal principal, string? title, CancellationToken cancellationToken = default)
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedTitle))
        {
            return CreateServiceRequestResult.ValidationFailure(
                message: "Request title is required.",
                errorCode: "VALIDATION_TITLE_REQUIRED");
        }

        if (normalizedTitle.Length > MaxTitleLength)
        {
            return CreateServiceRequestResult.ValidationFailure(
                message: $"Request title exceeds maximum length of {MaxTitleLength} characters.",
                errorCode: "VALIDATION_TITLE_TOO_LONG");
        }

        var request = new ServiceRequest(
            id: Guid.NewGuid(),
            tenantId: principal.TenantId,
            customerUserId: principal.UserId,
            title: normalizedTitle);

        await this.serviceRequestRepository.AddAsync(request, cancellationToken);
        await this.unitOfWork.SaveChangesAsync(cancellationToken);

        return CreateServiceRequestResult.Success(
            new CreatedServiceRequestPayload(
                RequestId: request.Id,
                TenantId: request.TenantId,
                CustomerUserId: request.CustomerUserId,
                Title: request.Title,
                Status: request.Status.ToString(),
                CreatedAtUtc: request.CreatedAtUtc,
                UpdatedAtUtc: request.UpdatedAtUtc));
    }
}
