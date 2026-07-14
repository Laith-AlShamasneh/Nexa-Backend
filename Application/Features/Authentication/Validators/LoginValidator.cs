using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.EmailRequired)
            .EmailAddress()
            .WithMessage(MessageKeys.Authentication.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.PasswordRequired);
    }
}
