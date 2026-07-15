using Application.Common.Upload;
using Application.Features.Tenancy.DTOs;

namespace WebApi.Endpoints.Tenancy;

/// <summary>
/// Transport-layer mirror of <see cref="RegisterOrganizationRequest"/>, bound from
/// <c>multipart/form-data</c> via <c>[FromForm]</c> — the one difference is
/// <see cref="Logo"/>, an actual <see cref="IFormFile"/> here versus the
/// transport-agnostic <see cref="FileUpload"/> Application expects (Application must
/// not reference ASP.NET Core types — see docs/ARCHITECTURE_RULES.md). This class
/// only maps field-for-field into the Application request; it carries no behavior
/// of its own.
/// </summary>
public sealed class RegisterOrganizationFormRequest
{
    public string      OrganizationName            { get; set; } = string.Empty;
    public string?     OrganizationArabicName      { get; set; }
    public string?     OrganizationLegalName       { get; set; }
    public string?     OrganizationArabicLegalName { get; set; }
    public IFormFile?  Logo                        { get; set; }
    public string?     OrganizationEmail           { get; set; }
    public string?     OrganizationPhone           { get; set; }
    public string?     OrganizationAddress         { get; set; }

    public string TimeZoneId          { get; set; } = string.Empty;
    public string DefaultLanguageCode { get; set; } = string.Empty;
    public string CurrencyCode        { get; set; } = string.Empty;

    public string  BranchName       { get; set; } = string.Empty;
    public string? BranchArabicName { get; set; }
    public string? BranchPhone      { get; set; }
    public string? BranchEmail      { get; set; }
    public string? BranchAddress    { get; set; }

    public string  FirstName       { get; set; } = string.Empty;
    public string  LastName        { get; set; } = string.Empty;
    public string? ArabicFirstName { get; set; }
    public string? ArabicLastName  { get; set; }
    public string  Username        { get; set; } = string.Empty;
    public string  Email           { get; set; } = string.Empty;
    public string? Phone           { get; set; }
    public string  Password        { get; set; } = string.Empty;
    public string  ConfirmPassword { get; set; } = string.Empty;

    public RegisterOrganizationRequest ToApplicationRequest() => new()
    {
        OrganizationName            = OrganizationName,
        OrganizationArabicName      = OrganizationArabicName,
        OrganizationLegalName       = OrganizationLegalName,
        OrganizationArabicLegalName = OrganizationArabicLegalName,
        Logo                        = Logo is null ? null : new FileUpload
        {
            FileName    = Logo.FileName,
            ContentType = Logo.ContentType,
            Length      = Logo.Length,
            Content     = Logo.OpenReadStream()
        },
        OrganizationEmail   = OrganizationEmail,
        OrganizationPhone   = OrganizationPhone,
        OrganizationAddress = OrganizationAddress,

        TimeZoneId          = TimeZoneId,
        DefaultLanguageCode = DefaultLanguageCode,
        CurrencyCode        = CurrencyCode,

        BranchName       = BranchName,
        BranchArabicName = BranchArabicName,
        BranchPhone      = BranchPhone,
        BranchEmail      = BranchEmail,
        BranchAddress    = BranchAddress,

        FirstName       = FirstName,
        LastName        = LastName,
        ArabicFirstName = ArabicFirstName,
        ArabicLastName  = ArabicLastName,
        Username        = Username,
        Email           = Email,
        Phone           = Phone,
        Password        = Password,
        ConfirmPassword = ConfirmPassword
    };
}
