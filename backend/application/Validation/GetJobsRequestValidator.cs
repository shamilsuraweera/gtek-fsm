using FluentValidation;
using GTEK.FSM.Backend.Domain.Enums;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Jobs.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class GetJobsRequestValidator : AbstractValidator<GetJobsRequest>
{
    public GetJobsRequestValidator()
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

        RuleFor(x => x.WorkerIdFilter)
            .Must(BeGuid)
            .When(x => !string.IsNullOrWhiteSpace(x.WorkerIdFilter))
            .WithMessage("workerIdFilter must be a valid guid.");

        RuleFor(x => x.SortDirection)
            .Must(BeValidSortDirection)
            .When(x => !string.IsNullOrWhiteSpace(x.SortDirection))
            .WithMessage("sortDirection must be asc or desc.");

        RuleFor(x => x)
            .Must(HaveValidDateRange)
            .WithMessage("scheduledFromUtc must be less than or equal to scheduledToUtc.");
    }

    private static bool BeValidStatus(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && Enum.TryParse<AssignmentStatus>(value.Trim(), ignoreCase: true, out _);
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

    private static bool HaveValidDateRange(GetJobsRequest request)
    {
        if (!request.ScheduledFromUtc.HasValue || !request.ScheduledToUtc.HasValue)
        {
            return true;
        }

        return request.ScheduledFromUtc.Value <= request.ScheduledToUtc.Value;
    }
}
