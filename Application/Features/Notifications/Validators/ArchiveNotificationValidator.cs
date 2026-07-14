using Application.Features.Notifications.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Notifications.Validators;

public sealed class ArchiveNotificationValidator : AbstractValidator<ArchiveNotificationRequest>
{
    public ArchiveNotificationValidator()
    {
        RuleFor(x => x.NotificationId)
            .GreaterThan(0)
            .WithMessage(MessageKeys.Notifications.InvalidNotificationId);
    }
}
