using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class GetRequestsRequestValidator : AbstractValidator<GetRequestsRequest>
{
    public GetRequestsRequestValidator()
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

        RuleFor(x => x.StatusFilter)
            .Must(BeValidStatus)
            .When(x => !string.IsNullOrWhiteSpace(x.StatusFilter))
            .WithMessage("statusFilter is invalid.");

        RuleFor(x => x.AssignedWorkerUserIdFilter)
            .Must(BeGuid)
            .When(x => !string.IsNullOrWhiteSpace(x.AssignedWorkerUserIdFilter))
            .WithMessage("assignedWorkerUserIdFilter must be a valid guid.");

        RuleFor(x => x.SortDirection)
            .Must(BeValidSortDirection)
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("sortDirection must be asc or desc.");

        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("createdFromUtc must be less than or equal to createdToUtc.");
    }

    private static bool BeValidStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<ServiceRequestStatus>(value.Trim(), ignoreCase: true, out _);
    }

    private static bool BeGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }

    private static bool BeValidSortDirection(string? value)
    {
        return string.Equals(value, "asc", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "desc", StringComparison.OrdinalIgnoreCase);
    }

    private static bool HaveValidDateRange(GetRequestsRequest request)
    {
        if (!request.CreatedFromUtc.HasValue || !request.CreatedToUtc.HasValue)
        {
            return true;
        }

        return request.CreatedFromUtc.Value <= request.CreatedToUtc.Value;
    }
}
