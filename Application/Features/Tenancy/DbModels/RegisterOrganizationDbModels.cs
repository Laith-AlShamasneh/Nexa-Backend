namespace Application.Features.Tenancy.DbModels;

/// <summary>
/// Everything the <c>tenant.usp_Organization_Register</c> stored procedure needs.
/// Every backend-generated value (IDs, password hash, token hash/expiry) is computed
/// by Application/Infrastructure *before* this is built — the CPU work (password
/// hashing, token generation) happens before the database round trip, per
/// docs/TENANT_ONBOARDING.md "Performance Requirements".
/// </summary>
public sealed class RegisterOrganizationDbInput
{
    public required Guid OrganizationId { get; init; }
    public required string OrganizationName { get; init; }
    public string? OrganizationArabicName { get; init; }
    public string? OrganizationLegalName { get; init; }
    public string? OrganizationArabicLegalName { get; init; }
    public required string Slug { get; init; }
    public string? LogoUrl { get; init; }
    public string? OrganizationEmail { get; init; }
    public string? OrganizationPhone { get; init; }
    public string? OrganizationAddress { get; init; }

    public required string TimeZoneId { get; init; }
    public required string DefaultLanguageCode { get; init; }
    public required string CurrencyCode { get; init; }

    public required Guid BranchId { get; init; }
    public required string BranchName { get; init; }
    public string? BranchArabicName { get; init; }
    public string? BranchPhone { get; init; }
    public string? BranchEmail { get; init; }
    public string? BranchAddress { get; init; }

    public required Guid PersonId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public string? ArabicFirstName { get; init; }
    public string? ArabicLastName { get; init; }
    public string? OwnerPhone { get; init; }

    public required Guid UserId { get; init; }
    public required string Username { get; init; }
    public required string Email { get; init; }
    public required string PasswordHash { get; init; }

    public required string EmailConfirmationTokenHash { get; init; }
    public required DateTime EmailConfirmationExpiresAtUtc { get; init; }

    public string? CreatedByIp { get; init; }
    public Guid? CorrelationId { get; init; }
}

/// <summary>
/// One row back from the stored procedure. <see cref="ResultCode"/>: 0 = success,
/// 1 = organization slug conflict, 2 = required global role templates are missing
/// (a seed-data/deployment defect, not a user error). All other fields are null
/// unless <see cref="ResultCode"/> is 0.
/// </summary>
public sealed class RegisterOrganizationDbResult
{
    public int ResultCode { get; init; }
    public Guid? OrganizationId { get; init; }
    public Guid? BranchId { get; init; }
    public Guid? OwnerPersonId { get; init; }
    public Guid? OwnerUserId { get; init; }
    public Guid? OwnerRoleId { get; init; }
    public long? EmailConfirmationTokenId { get; init; }
    public DateTime? CreatedAt { get; init; }
}
