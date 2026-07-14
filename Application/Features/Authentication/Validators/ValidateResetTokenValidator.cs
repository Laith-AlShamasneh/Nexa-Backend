using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class ValidateResetTokenValidator : AbstractValidator<ValidateResetTokenRequest>
{
    public ValidateResetTokenValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(MessageKeys.Authentication.ResetTokenRequired);
    }
}
