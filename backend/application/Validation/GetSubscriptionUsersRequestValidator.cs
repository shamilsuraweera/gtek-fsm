using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Subscriptions.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class GetSubscriptionUsersRequestValidator : AbstractValidator<GetSubscriptionUsersRequest>
{
    public GetSubscriptionUsersRequestValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .When(x => x.Page.HasValue)
            .WithMessage("page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0)
            .When(x => x.PageSize.HasValue)
            .WithMessage("pageSize must be greater than 0.")
            .LessThanOrEqualTo(200)
            .When(x => x.PageSize.HasValue)
            .WithMessage("pageSize must be less than or equal to 200.");

        RuleFor(x => x.SearchText)
            .MaximumLength(180)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchText))
            .WithMessage("searchText exceeds maximum length of 180 characters.");
    }
}
