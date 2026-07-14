using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class ConfirmEmailValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(MessageKeys.Authentication.ConfirmationTokenRequired);
    }
}
