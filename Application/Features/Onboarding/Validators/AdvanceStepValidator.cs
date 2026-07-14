using Application.Features.Onboarding.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Onboarding.Validators;

public sealed class AdvanceStepValidator : AbstractValidator<AdvanceStepRequest>
{
    public AdvanceStepValidator()
    {
        RuleFor(x => x.StepKey)
            .NotEmpty()
            .WithMessage(MessageKeys.Common.BadRequest);
    }
}
