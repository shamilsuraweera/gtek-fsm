using FluentValidation;
using GTEK.FSM.Shared.Contracts.Api.Contracts.Auth.Requests;

namespace GTEK.FSM.Backend.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("email is required.")
            .EmailAddress()
            .WithMessage("email must be a valid email address.")
            .MaximumLength(256)
            .WithMessage("email must be 256 characters or fewer.");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("password is required.")
            .MinimumLength(8)
            .WithMessage("password must be at least 8 characters.")
            .MaximumLength(128)
            .WithMessage("password must be 128 characters or fewer.");
    }
}
