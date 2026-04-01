using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Categories.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class ReorderCategoriesRequestValidator : AbstractValidator<ReorderCategoriesRequest>
{
    public ReorderCategoriesRequestValidator()
    {
        RuleFor(x => x.Items)
            .NotEmpty()
            .WithMessage("items is required.");

        RuleForEach(x => x.Items)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.CategoryId)
                    .NotEmpty()
                    .WithMessage("categoryId is required.")
                    .Must(BeGuid)
                    .WithMessage("categoryId must be a valid guid.");

                item.RuleFor(i => i.SortOrder)
                    .NotNull()
                    .WithMessage("sortOrder is required.")
                    .GreaterThanOrEqualTo(0)
                    .WithMessage("sortOrder must be greater than or equal to 0.");
            });
    }

    private static bool BeGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }
}
