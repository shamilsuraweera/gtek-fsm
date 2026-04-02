using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class CreateWorkerProfileRequestValidator : AbstractValidator<CreateWorkerProfileRequest>
{
    public CreateWorkerProfileRequestValidator()
    {
        RuleFor(x => x.WorkerCode)
            .NotEmpty()
            .WithMessage("workerCode is required.")
            .MaximumLength(32)
            .WithMessage("workerCode must be 32 characters or fewer.");

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("displayName is required.")
            .MaximumLength(120)
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

        RuleFor(x => x.BaseLatitude)
            .InclusiveBetween(-90m, 90m)
            .When(x => x.BaseLatitude.HasValue)
            .WithMessage("baseLatitude must be between -90 and 90.");

        RuleFor(x => x.BaseLongitude)
            .InclusiveBetween(-180m, 180m)
            .When(x => x.BaseLongitude.HasValue)
            .WithMessage("baseLongitude must be between -180 and 180.");

        RuleFor(x => x)
            .Must(HaveBothCoordinatesOrNeither)
            .WithMessage("baseLatitude and baseLongitude must be supplied together.");
    }

    private static bool HaveBothCoordinatesOrNeither(CreateWorkerProfileRequest request)
    {
        return request.BaseLatitude.HasValue == request.BaseLongitude.HasValue;
    }

    private static bool BeAvailabilityValue(string? value)
    {
        return Enum.TryParse<WorkerAvailabilityStatus>(value, ignoreCase: true, out _);
    }
}
