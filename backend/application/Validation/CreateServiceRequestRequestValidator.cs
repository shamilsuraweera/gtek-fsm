using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class CreateServiceRequestRequestValidator : AbstractValidator<CreateServiceRequestRequest>
{
    public CreateServiceRequestRequestValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Request title is required.")
            .MaximumLength(180)
            .WithMessage("Request title exceeds maximum length of 180 characters.");
    }
}
