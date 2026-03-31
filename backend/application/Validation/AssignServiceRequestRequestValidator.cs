using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class AssignServiceRequestRequestValidator : AbstractValidator<AssignServiceRequestRequest>
{
    public AssignServiceRequestRequestValidator()
    {
        RuleFor(x => x.WorkerUserId)
            .NotEmpty()
            .WithMessage("Worker user id is required and must be a valid guid.")
            .Must(BeGuid)
            .WithMessage("Worker user id is required and must be a valid guid.");
    }

    private static bool BeGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }
}
