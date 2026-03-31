using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class UpdateOrganizationSubscriptionRequestValidator : AbstractValidator<UpdateOrganizationSubscriptionRequest>
{
    private static readonly string[] AllowedPlans = ["FREE", "PRO", "ENTERPRISE"];

    public UpdateOrganizationSubscriptionRequestValidator()
    {
        RuleFor(x => x.PlanCode)
            .NotEmpty()
            .WithMessage("planCode must be one of FREE, PRO, ENTERPRISE.")
            .Must(BeAllowedPlan)
            .WithMessage("planCode must be one of FREE, PRO, ENTERPRISE.");

        RuleFor(x => x.UserLimit)
            .NotNull()
            .WithMessage("userLimit is required.")
            .GreaterThan(0)
            .WithMessage("userLimit must be greater than 0.");
    }

    private static bool BeAllowedPlan(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && AllowedPlans.Contains(value.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);
    }
}
