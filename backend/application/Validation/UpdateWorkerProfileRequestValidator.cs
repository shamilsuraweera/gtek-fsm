using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class UpdateWorkerProfileRequestValidator : AbstractValidator<UpdateWorkerProfileRequest>
{
    public UpdateWorkerProfileRequestValidator()
    {
        RuleFor(x => x.WorkerCode)
            .MaximumLength(32)
            .When(x => !string.IsNullOrWhiteSpace(x.WorkerCode))
            .WithMessage("workerCode must be 32 characters or fewer.");

        RuleFor(x => x.DisplayName)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.DisplayName))
            .WithMessage("displayName must be 120 characters or fewer.");

        RuleFor(x => x.InternalRating)
            .InclusiveBetween(1.0m, 5.0m)
            .When(x => x.InternalRating.HasValue)
            .WithMessage("internalRating must be between 1.0 and 5.0.");

        RuleForEach(x => x.Skills)
            .NotEmpty()
            .WithMessage("skills entries cannot be empty.")
            .MaximumLength(40)
            .WithMessage("skill entries must be 40 characters or fewer.");

        RuleFor(x => x.Skills)
            .Must(x => x is null || x.Length <= 20)
            .WithMessage("skills cannot contain more than 20 entries.");

        RuleFor(x => x.AvailabilityStatus)
            .Must(BeAvailabilityValue)
            .When(x => !string.IsNullOrWhiteSpace(x.AvailabilityStatus))
            .WithMessage("availabilityStatus is invalid.");
    }

    private static bool BeAvailabilityValue(string? value)
    {
        return Enum.TryParse<WorkerAvailabilityStatus>(value, ignoreCase: true, out _);
    }
}
