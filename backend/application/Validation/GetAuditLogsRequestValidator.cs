using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Audit.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class GetAuditLogsRequestValidator : AbstractValidator<GetAuditLogsRequest>
{
    public GetAuditLogsRequestValidator()
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

        RuleFor(x => x.ActorUserId)
            .Must(BeGuid)
            .When(x => !string.IsNullOrWhiteSpace(x.ActorUserId))
            .WithMessage("actorUserId must be a valid guid.");

        RuleFor(x => x.EntityId)
            .Must(BeGuid)
            .When(x => !string.IsNullOrWhiteSpace(x.EntityId))
            .WithMessage("entityId must be a valid guid.");

        RuleFor(x => x.EntityType)
            .MaximumLength(120)
            .When(x => !string.IsNullOrWhiteSpace(x.EntityType))
            .WithMessage("entityType exceeds maximum length of 120 characters.");

        RuleFor(x => x.Action)
            .MaximumLength(180)
            .When(x => !string.IsNullOrWhiteSpace(x.Action))
            .WithMessage("action exceeds maximum length of 180 characters.");

        RuleFor(x => x.Outcome)
            .MaximumLength(80)
            .When(x => !string.IsNullOrWhiteSpace(x.Outcome))
            .WithMessage("outcome exceeds maximum length of 80 characters.");

        RuleFor(x => x)
            .Must(x => !x.FromUtc.HasValue || !x.ToUtc.HasValue || x.FromUtc.Value <= x.ToUtc.Value)
            .WithMessage("fromUtc must be less than or equal to toUtc.");
    }

    private static bool BeGuid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && Guid.TryParse(value, out _);
    }
}
