namespace Application.Features.Email.Jobs;

public sealed record WorkspaceInvitationEmailPayload(
    string  ToEmail,
    string  InviterNameEn,
    string  InviterNameAr,
    string  WorkspaceName,
    string  RoleCode,
    string  AcceptToken,
    string  RejectToken,        // same token — front-end path determines action
    string  AcceptLink,         // fully-built FE accept URL (base + token), like ResetLink
    DateTime ExpiresAtUtc,
    string  Language            // "en" | "ar"
);
