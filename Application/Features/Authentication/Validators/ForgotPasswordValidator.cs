using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class ForgotPasswordValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(MessageKeys.Authentication.EmailRequired)
            .EmailAddress().WithMessage(MessageKeys.Authentication.InvalidEmail);
    }
}
