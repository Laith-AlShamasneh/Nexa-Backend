namespace Application.Features.EmailConfirmation.DbModels;

public sealed class ConfirmEmailDbInput
{
    public required string TokenHash { get; init; }
    public string?  UsedByIp     { get; init; }
    public Guid?    CorrelationId { get; init; }
}

/// <summary>
/// ResultCode: 0 = Confirmed (just now), 1 = AlreadyConfirmed (idempotent — same
/// public outcome as 0), 2 = Invalid (token not found/expired/revoked/used-by-an-
/// ineligible-user, or the associated user/organization isn't eligible). See
/// identity.usp_EmailConfirmation_Confirm (migration 013) for the exact mapping —
/// deliberately collapsed to these 3 codes so nothing upstream can leak which
/// specific sub-case applied.
/// </summary>
public sealed class ConfirmEmailDbResult
{
    public int   ResultCode     { get; init; }
    public Guid? UserId         { get; init; }
    public Guid? OrganizationId { get; init; }
}

public sealed class ResendEmailConfirmationDbInput
{
    public required string   Email                 { get; init; }
    public required string   NewTokenHash          { get; init; }
    public required DateTime NewTokenExpiresAtUtc  { get; init; }
    public required int      ResendCooldownSeconds { get; init; }
    public required int      MaxResendsPerHour     { get; init; }
    public string?  RequestIp     { get; init; }
    public Guid?    CorrelationId { get; init; }
}

/// <summary>
/// ResultCode: 0 = TokenCreated, 1 = NotEligible, 2 = CooldownActive,
/// 3 = MaxResendsPerHourExceeded. Only ResultCode 0 means a token was actually
/// persisted — the caller must only enqueue the confirmation email in that case.
/// Every other code maps to the exact same generic public response.
/// </summary>
public sealed class ResendEmailConfirmationDbResult
{
    public int     ResultCode     { get; init; }
    public Guid?    UserId         { get; init; }
    public Guid?    OrganizationId { get; init; }
    public string?  DisplayNameEn  { get; init; }
    public string?  DisplayNameAr  { get; init; }
}
