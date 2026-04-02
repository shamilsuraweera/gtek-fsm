using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Workers.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class GetWorkersRequestValidator : AbstractValidator<GetWorkersRequest>
{
    public GetWorkersRequestValidator()
    {
        RuleFor(x => x.SearchText)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.SearchText))
            .WithMessage("searchText must be 120 characters or fewer.");

        RuleFor(x => x.Page)
            .GreaterThan(0)
            .When(x => x.Page.HasValue)
            .WithMessage("page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .When(x => x.PageSize.HasValue)
            .WithMessage("pageSize must be between 1 and 100.");
    }
}
