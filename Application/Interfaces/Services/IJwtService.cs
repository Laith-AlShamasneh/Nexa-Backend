using System.Security.Claims;
using Application.Features.Authentication.DTOs;

namespace Application.Interfaces.Services;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) GenerateAccessToken(JwtTokenResponse model);
    ClaimsPrincipal? GetPrincipalFromToken(string token);
}
