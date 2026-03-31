using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class TransitionServiceRequestStatusRequestValidator : AbstractValidator<TransitionServiceRequestStatusRequest>
{
    public TransitionServiceRequestStatusRequestValidator()
    {
        RuleFor(x => x.NextStatus)
            .NotEmpty()
            .WithMessage("Next status is required.")
            .Must(BeValidServiceRequestStatus)
            .WithMessage("Requested status is invalid.");
    }

    private static bool BeValidServiceRequestStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<ServiceRequestStatus>(value.Trim(), ignoreCase: true, out _);
    }
}
