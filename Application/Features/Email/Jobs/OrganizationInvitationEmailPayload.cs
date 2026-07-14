namespace Application.Features.Email.Jobs;

public sealed record OrganizationInvitationEmailPayload(
    string   ToEmail,
    string   InviterNameEn,
    string   InviterNameAr,
    string   OrganizationName,
    string   RoleName,
    string   AcceptToken,
    string   AcceptLink,          // fully-built FE accept URL (base + token), like ResetLink
    DateTime ExpiresAtUtc,
    string   Language             // "en" | "ar"
);
