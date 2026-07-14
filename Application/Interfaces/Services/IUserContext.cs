using Shared.Enums.System;

namespace Application.Interfaces.Services;

public interface IUserContext
{
    long UserId { get; }

    /// <summary>
    /// The tenant (organization) the request is acting in, resolved from the
    /// authenticated JWT's "org_id" claim — never from a request header or body.
    /// A user's token is only ever issued for one organization (see
    /// docs/MULTI_TENANCY.md), so this is non-null for any authenticated request
    /// and every tenant-scoped stored procedure must filter by it.
    /// </summary>
    Guid? OrganizationId { get; }

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
