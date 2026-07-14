using Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Shared.Enums.System;
using System.Security.Claims;

namespace Infrastructure.Services.Authentication;

internal sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    private HttpContext? Http => httpContextAccessor.HttpContext;

    public long UserId
    {
        get
        {
            var claim = Http?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return long.TryParse(claim, out var id) ? id : 0;
        }
    }

    public long? WorkspaceId
    {
        get
        {
            var header = Http?.Request.Headers["X-Workspace-Id"].FirstOrDefault();
            return long.TryParse(header, out var id) && id > 0 ? id : null;
        }
    }

    public string Email => Http?.User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    // The JWT stores the display name under "preferred_username"
    // (JwtRegisteredClaimNames.PreferredUsername); it is NOT in the default inbound
    // claim map, so it is never remapped to ClaimTypes.Name. Read it directly, with a
    // fallback to ClaimTypes.Name for any principal that does use the standard claim.
    public string DisplayName =>
        Http?.User.FindFirst("preferred_username")?.Value
        ?? Http?.User.FindFirst(ClaimTypes.Name)?.Value
        ?? string.Empty;

    public bool IsAuthenticated => Http?.User.Identity?.IsAuthenticated ?? false;

    public SystemRoles RoleId
    {
        get
        {
            var claim = Http?.User.FindFirst(ClaimTypes.Role)?.Value;
            return int.TryParse(claim, out var roleId) && Enum.IsDefined(typeof(SystemRoles), roleId)
                ? (SystemRoles)roleId
                : default;
        }
    }

    public SystemLanguages Language
    {
        get
        {
            var header = Http?.Request.Headers["Accept-Language"].ToString();
            return header?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true
                ? SystemLanguages.English
                : SystemLanguages.Arabic;
        }
    }

    public string? IpAddress     => Http?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent     => Http?.Request.Headers["User-Agent"].ToString();
    public string? SessionId     => Http?.User.FindFirst("session_id")?.Value
                                 ?? Http?.Request.Headers["X-Session-Id"].FirstOrDefault();
    public string? TraceId       => Http?.TraceIdentifier
                                 ?? Http?.Request.Headers["X-Trace-Id"].FirstOrDefault()
                                 ?? Guid.NewGuid().ToString();

    public string RequestBaseUrl
    {
        get
        {
            if (Http is null) return string.Empty;
            var req = Http.Request;
            return $"{req.Scheme}://{req.Host}";
        }
    }
}
