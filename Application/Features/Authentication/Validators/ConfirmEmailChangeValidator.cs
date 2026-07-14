using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class ConfirmEmailChangeValidator : AbstractValidator<ConfirmEmailChangeRequest>
{
    public ConfirmEmailChangeValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(MessageKeys.Profile.EmailChangeTokenRequired);
    }
}
