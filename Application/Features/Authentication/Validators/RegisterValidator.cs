using Application.Common.Upload;
using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{

    public RegisterValidator()
    {
        RuleFor(x => x.FirstNameEn)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.FirstNameRequired)
            .MaximumLength(100)
            .WithMessage(MessageKeys.Authentication.FirstNameTooLong);

        RuleFor(x => x.LastNameEn)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.LastNameRequired)
            .MaximumLength(100)
            .WithMessage(MessageKeys.Authentication.LastNameTooLong);

        RuleFor(x => x.DisplayNameEn)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.DisplayNameRequired)
            .MaximumLength(200)
            .WithMessage(MessageKeys.Authentication.DisplayNameTooLong);

        RuleFor(x => x.FirstNameAr)
            .MaximumLength(100)
            .WithMessage(MessageKeys.Authentication.FirstNameTooLong)
            .When(x => !string.IsNullOrEmpty(x.FirstNameAr));

        RuleFor(x => x.LastNameAr)
            .MaximumLength(100)
            .WithMessage(MessageKeys.Authentication.LastNameTooLong)
            .When(x => !string.IsNullOrEmpty(x.LastNameAr));

        RuleFor(x => x.DisplayNameAr)
            .MaximumLength(200)
            .WithMessage(MessageKeys.Authentication.DisplayNameTooLong)
            .When(x => !string.IsNullOrEmpty(x.DisplayNameAr));

        RuleFor(x => x.DateOfBirth)
            .Must(d => d!.Value <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(MessageKeys.Authentication.InvalidDateOfBirth)
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.EmailRequired)
            .EmailAddress()
            .WithMessage(MessageKeys.Authentication.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage(MessageKeys.Authentication.PasswordRequired)
            .MinimumLength(8)
            .WithMessage(MessageKeys.Authentication.PasswordTooShort)
            .Matches("[A-Z]")
            .WithMessage(MessageKeys.Authentication.PasswordUppercaseRequired)
            .Matches("[a-z]")
            .WithMessage(MessageKeys.Authentication.PasswordLowercaseRequired)
            .Matches("[0-9]")
            .WithMessage(MessageKeys.Authentication.PasswordDigitRequired)
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]")
            .WithMessage(MessageKeys.Authentication.PasswordSpecialRequired);

        RuleFor(x => x.ProfileImage)
            .Must(f =>
            {
                var policy = UploadPolicies.ProfileImage;
                var ext    = Path.GetExtension(f!.FileName);
                return policy.AllowedMimeTypes.Contains(f.ContentType, StringComparer.OrdinalIgnoreCase)
                    && policy.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage(MessageKeys.Authentication.InvalidProfileImageFormat)
            .When(x => x.ProfileImage is not null)
            .DependentRules(() =>
            {
                RuleFor(x => x.ProfileImage)
                    .Must(f => f!.Length <= UploadPolicies.ProfileImage.MaxSizeBytes)
                    .WithMessage(MessageKeys.Authentication.ProfileImageTooLarge)
                    .When(x => x.ProfileImage is not null);
            });
    }
}
