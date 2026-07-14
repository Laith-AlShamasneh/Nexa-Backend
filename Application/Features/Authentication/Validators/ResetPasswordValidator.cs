using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class ResetPasswordValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(MessageKeys.Authentication.ResetTokenRequired);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage(MessageKeys.Authentication.NewPasswordRequired)
            .MinimumLength(8).WithMessage(MessageKeys.Authentication.PasswordTooShort)
            .Matches("[A-Z]").WithMessage(MessageKeys.Authentication.PasswordUppercaseRequired)
            .Matches("[a-z]").WithMessage(MessageKeys.Authentication.PasswordLowercaseRequired)
            .Matches("[0-9]").WithMessage(MessageKeys.Authentication.PasswordDigitRequired)
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage(MessageKeys.Authentication.PasswordSpecialRequired);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(MessageKeys.Authentication.ConfirmPasswordRequired)
            .Equal(x => x.NewPassword).WithMessage(MessageKeys.Authentication.PasswordMismatch);
    }
}
