using System.Text.RegularExpressions;
using Application.Common.Upload;
using Application.Features.Tenancy.DTOs;
using Domain.Identity.Constants;
using Domain.Tenancy.Constants;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Tenancy.Validators;

public sealed partial class RegisterOrganizationValidator : AbstractValidator<RegisterOrganizationRequest>
{
    public RegisterOrganizationValidator()
    {
        // ── Organization ─────────────────────────────────────────────────────
        RuleFor(x => x.OrganizationName)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.OrganizationNameRequired)
            .MaximumLength(TenancyLengths.Organization.NameMaxLength).WithMessage(MessageKeys.Tenancy.OrganizationNameRequired);

        RuleFor(x => x.OrganizationArabicName)
            .MaximumLength(TenancyLengths.Organization.NameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.OrganizationArabicName));

        RuleFor(x => x.OrganizationLegalName)
            .MaximumLength(TenancyLengths.Organization.NameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.OrganizationLegalName));

        RuleFor(x => x.OrganizationEmail)
            .EmailAddress().WithMessage(MessageKeys.Tenancy.InvalidOrganizationEmail)
            .MaximumLength(TenancyLengths.Organization.EmailMaxLength)
            .When(x => !string.IsNullOrEmpty(x.OrganizationEmail));

        RuleFor(x => x.OrganizationPhone)
            .MaximumLength(TenancyLengths.Organization.PhoneMaxLength)
            .When(x => !string.IsNullOrEmpty(x.OrganizationPhone));

        RuleFor(x => x.OrganizationAddress)
            .MaximumLength(TenancyLengths.Organization.AddressMaxLength)
            .When(x => !string.IsNullOrEmpty(x.OrganizationAddress));

        RuleFor(x => x.Logo)
            .Must(f =>
            {
                var policy = UploadPolicies.OrganizationLogo;
                var ext    = Path.GetExtension(f!.FileName);
                return policy.AllowedMimeTypes.Contains(f.ContentType, StringComparer.OrdinalIgnoreCase)
                    && policy.AllowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
            })
            .WithMessage(MessageKeys.Tenancy.InvalidLogoFormat)
            .When(x => x.Logo is not null)
            .DependentRules(() =>
            {
                RuleFor(x => x.Logo)
                    .Must(f => f!.Length <= UploadPolicies.OrganizationLogo.MaxSizeBytes)
                    .WithMessage(MessageKeys.Tenancy.LogoTooLarge)
                    .When(x => x.Logo is not null);
            });

        // ── Organization settings ────────────────────────────────────────────
        RuleFor(x => x.TimeZoneId)
            .NotEmpty()
            .Must(BeAKnownTimeZone).WithMessage(MessageKeys.Tenancy.UnsupportedTimeZone);

        RuleFor(x => x.DefaultLanguageCode)
            .NotEmpty()
            .Matches(LanguageCodePattern()).WithMessage(MessageKeys.Tenancy.UnsupportedLanguageCode)
            .MaximumLength(TenancyLengths.OrganizationSettings.DefaultLanguageCodeMaxLength);

        RuleFor(x => x.CurrencyCode)
            .NotEmpty()
            .Matches(CurrencyCodePattern()).WithMessage(MessageKeys.Tenancy.UnsupportedCurrencyCode);

        // ── Main branch ───────────────────────────────────────────────────────
        RuleFor(x => x.BranchName)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.BranchNameRequired)
            .MaximumLength(TenancyLengths.Branch.NameMaxLength).WithMessage(MessageKeys.Tenancy.BranchNameRequired);

        RuleFor(x => x.BranchArabicName)
            .MaximumLength(TenancyLengths.Branch.NameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.BranchArabicName));

        RuleFor(x => x.BranchEmail)
            .EmailAddress()
            .MaximumLength(TenancyLengths.Branch.EmailMaxLength)
            .When(x => !string.IsNullOrEmpty(x.BranchEmail));

        RuleFor(x => x.BranchPhone)
            .MaximumLength(TenancyLengths.Branch.PhoneMaxLength)
            .When(x => !string.IsNullOrEmpty(x.BranchPhone));

        RuleFor(x => x.BranchAddress)
            .MaximumLength(TenancyLengths.Branch.AddressMaxLength)
            .When(x => !string.IsNullOrEmpty(x.BranchAddress));

        // ── Owner ─────────────────────────────────────────────────────────────
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.OwnerFirstNameRequired)
            .MaximumLength(IdentityLengths.Person.FirstNameMaxLength).WithMessage(MessageKeys.Tenancy.OwnerFirstNameRequired);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.OwnerLastNameRequired)
            .MaximumLength(IdentityLengths.Person.LastNameMaxLength).WithMessage(MessageKeys.Tenancy.OwnerLastNameRequired);

        RuleFor(x => x.ArabicFirstName)
            .MaximumLength(IdentityLengths.Person.FirstNameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.ArabicFirstName));

        RuleFor(x => x.ArabicLastName)
            .MaximumLength(IdentityLengths.Person.LastNameMaxLength)
            .When(x => !string.IsNullOrEmpty(x.ArabicLastName));

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.OwnerUsernameRequired)
            .MaximumLength(IdentityLengths.User.UsernameMaxLength).WithMessage(MessageKeys.Tenancy.OwnerUsernameRequired)
            .Matches(UsernamePattern()).WithMessage(MessageKeys.Tenancy.OwnerUsernameRequired);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(MessageKeys.Tenancy.OwnerEmailRequired)
            .EmailAddress().WithMessage(MessageKeys.Tenancy.InvalidOwnerEmail)
            .MaximumLength(IdentityLengths.User.EmailMaxLength).WithMessage(MessageKeys.Tenancy.InvalidOwnerEmail);

        RuleFor(x => x.Phone)
            .MaximumLength(IdentityLengths.Person.PhoneMaxLength)
            .When(x => !string.IsNullOrEmpty(x.Phone));

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(MessageKeys.Authentication.PasswordRequired)
            .MinimumLength(8).WithMessage(MessageKeys.Authentication.PasswordTooShort)
            .Matches("[A-Z]").WithMessage(MessageKeys.Authentication.PasswordUppercaseRequired)
            .Matches("[a-z]").WithMessage(MessageKeys.Authentication.PasswordLowercaseRequired)
            .Matches("[0-9]").WithMessage(MessageKeys.Authentication.PasswordDigitRequired)
            .Matches(@"[!@#$%^&*(),.?""':{}|<>]").WithMessage(MessageKeys.Authentication.PasswordSpecialRequired);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithMessage(MessageKeys.Tenancy.PasswordMismatch);
    }

    private static bool BeAKnownTimeZone(string timeZoneId)
    {
        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return true;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    [GeneratedRegex(@"^[a-z]{2}(-[A-Z]{2})?$")]
    private static partial Regex LanguageCodePattern();

    [GeneratedRegex(@"^[A-Z]{3}$")]
    private static partial Regex CurrencyCodePattern();

    [GeneratedRegex(@"^[a-zA-Z0-9._-]+$")]
    private static partial Regex UsernamePattern();
}
