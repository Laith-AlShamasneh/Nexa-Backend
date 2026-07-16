using Application.Features.EmailConfirmation.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.EmailConfirmation.Validators;

public sealed class ResendEmailConfirmationValidator : AbstractValidator<ResendEmailConfirmationRequest>
{
    public ResendEmailConfirmationValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(MessageKeys.Authentication.EmailRequired)
            .EmailAddress().WithMessage(MessageKeys.Authentication.InvalidEmail);
    }
}
