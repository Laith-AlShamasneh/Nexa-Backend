using Application.Features.Notifications.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Notifications.Validators;

public sealed class GetNotificationsValidator : AbstractValidator<GetNotificationsRequest>
{
    public GetNotificationsValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0)
            .WithMessage(MessageKeys.Notifications.InvalidPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50)
            .WithMessage(MessageKeys.Notifications.InvalidPageSize);

        When(x => x.Status.HasValue, () =>
        {
            RuleFor(x => x.Status!.Value)
                .Must(v => v is >= 1 and <= 4)
                .WithMessage(MessageKeys.Common.BadRequest);
        });

        When(x => x.Category.HasValue, () =>
        {
            RuleFor(x => x.Category!.Value)
                .Must(v => v is >= 1 and <= 5)
                .WithMessage(MessageKeys.Common.BadRequest);
        });
    }
}
