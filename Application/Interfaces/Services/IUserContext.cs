using Shared.Enums.System;

namespace Application.Interfaces.Services;

public interface IUserContext
{
    long UserId { get; }

    /// <summary>
    /// The workspace the request is acting in, from the X-Workspace-Id header.
    /// NULL means "no explicit workspace" — every scoped stored procedure resolves
    /// NULL to the caller's personal workspace, preserving pre-workspace behavior.
    /// Authorization for a non-personal workspace is enforced inside the SPs via
    /// fn_CanAccessWorkspace, so a forged/unauthorized id fails closed.
    /// </summary>
    long? WorkspaceId { get; }

    string Email { get; }
    string DisplayName { get; }
    bool IsAuthenticated { get; }
    SystemRoles RoleId { get; }
    SystemLanguages Language { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    string? SessionId { get; }
    string? TraceId { get; }
    string RequestBaseUrl { get; }
}
