namespace Application.Features.Authentication.DTOs;

public record JwtTokenResponse(
    long UserId,
    string Email,
    string DisplayName,
    IEnumerable<int> RoleIds,
    string? SecurityStamp = null
);