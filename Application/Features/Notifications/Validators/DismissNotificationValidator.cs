using Application.Features.Notifications.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Notifications.Validators;

public sealed class DismissNotificationValidator : AbstractValidator<DismissNotificationRequest>
{
    public DismissNotificationValidator()
    {
        RuleFor(x => x.NotificationId)
            .GreaterThan(0)
            .WithMessage(MessageKeys.Notifications.InvalidNotificationId);
    }
}
