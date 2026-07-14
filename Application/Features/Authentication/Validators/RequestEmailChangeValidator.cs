using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class RequestEmailChangeValidator : AbstractValidator<RequestEmailChangeRequest>
{
    public RequestEmailChangeValidator()
    {
        RuleFor(x => x.NewEmail)
            .NotEmpty().WithMessage(MessageKeys.Profile.NewEmailRequired)
            .EmailAddress().WithMessage(MessageKeys.Profile.NewEmailInvalid)
            .MaximumLength(254).WithMessage(MessageKeys.Profile.NewEmailTooLong);

        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage(MessageKeys.Profile.CurrentPasswordRequired);
    }
}
