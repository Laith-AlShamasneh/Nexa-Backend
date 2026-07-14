using Application.Features.Notifications.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Notifications.Validators;

public sealed class DeleteNotificationValidator : AbstractValidator<DeleteNotificationRequest>
{
    public DeleteNotificationValidator()
    {
        RuleFor(x => x.NotificationId)
            .GreaterThan(0)
            .WithMessage(MessageKeys.Notifications.InvalidNotificationId);
    }
}
