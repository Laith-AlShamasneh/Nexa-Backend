using Application.Common.Upload;

namespace Application.Features.Tenancy.DTOs;

/// <summary>
/// Public registration request. Deliberately excludes anything backend/database
/// generated (OrganizationId, PersonId, UserId, BranchId, RoleId, password hash,
/// normalized fields, audit fields, system-role flags) — see docs/TENANT_ONBOARDING.md
/// "Request Contract" for the full list and why. The tenant Slug is also not accepted
/// from the client; it is derived from <see cref="OrganizationName"/> internally
/// (see <c>SlugGenerator</c>) since the client shouldn't need to know this identifier
/// exists. Submitted as <c>multipart/form-data</c> (not JSON) because of
/// <see cref="Logo"/> — see WebApi/Endpoints/Tenancy/RegisterOrganizationFormRequest.
/// </summary>
public sealed class RegisterOrganizationRequest
{
    // Organization
    public string      OrganizationName            { get; set; } = string.Empty;
    public string?     OrganizationArabicName      { get; set; }
    public string?     OrganizationLegalName       { get; set; }
    public string?     OrganizationArabicLegalName { get; set; }
    public FileUpload? Logo                        { get; set; }
    public string?     OrganizationEmail           { get; set; }
    public string?     OrganizationPhone           { get; set; }
    public string?     OrganizationAddress         { get; set; }

    // Organization settings
    public string TimeZoneId           { get; set; } = string.Empty;
    public string DefaultLanguageCode  { get; set; } = string.Empty;
    public string CurrencyCode         { get; set; } = string.Empty;

    // Main branch
    public string  BranchName        { get; set; } = string.Empty;
    public string? BranchArabicName  { get; set; }
    public string? BranchPhone       { get; set; }
    public string? BranchEmail       { get; set; }
    public string? BranchAddress     { get; set; }

    // Owner
    public string  FirstName        { get; set; } = string.Empty;
    public string  LastName         { get; set; } = string.Empty;
    public string? ArabicFirstName  { get; set; }
    public string? ArabicLastName   { get; set; }
    public string  Username         { get; set; } = string.Empty;
    public string  Email            { get; set; } = string.Empty;
    public string? Phone            { get; set; }
    public string  Password         { get; set; } = string.Empty;
    public string  ConfirmPassword  { get; set; } = string.Empty;
}

/// <summary>
/// Public registration response. Deliberately excludes the password hash, the raw or
/// hashed email-confirmation token, and the owner's Role Id — none of these are the
/// caller's business. <see cref="EmailConfirmationRequired"/> is always true today
/// (there is no "skip confirmation" path), returned explicitly rather than assumed
/// so the frontend doesn't have to hardcode that assumption.
/// </summary>
public sealed record RegisterOrganizationResponse(
    Guid     OrganizationId,
    Guid     MainBranchId,
    Guid     OwnerUserId,
    string   OwnerEmail,
    bool     EmailConfirmationRequired,
    DateTime CreatedAt);
