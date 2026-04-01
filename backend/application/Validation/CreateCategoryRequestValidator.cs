using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("code is required.")
            .MaximumLength(32)
            .WithMessage("code must be 32 characters or fewer.");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("name is required.")
            .MaximumLength(120)
            .WithMessage("name must be 120 characters or fewer.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0)
            .When(x => x.SortOrder.HasValue)
            .WithMessage("sortOrder must be greater than or equal to 0.");
    }
}
