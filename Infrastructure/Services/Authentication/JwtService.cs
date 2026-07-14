using Application.Features.Authentication.DTOs;
using Application.Interfaces.Services;
using Infrastructure.Services.Authentication.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Infrastructure.Services.Authentication;

internal sealed class JwtService : IJwtService
{
    private readonly JwtOptions _options;
    private readonly TokenValidationParameters _validationParameters;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = _options.Issuer,
            ValidAudience            = _options.Audience,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey)),
            ClockSkew                = TimeSpan.Zero
        };
    }

    public (string Token, DateTime ExpiresAt) GenerateAccessToken(JwtTokenResponse model)
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(_options.ExpiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.NameId,             model.UserId.ToString()),
            new(JwtRegisteredClaimNames.Email,               model.Email),
            new(JwtRegisteredClaimNames.PreferredUsername,   model.DisplayName),
            new(JwtRegisteredClaimNames.Jti,                 Guid.NewGuid().ToString())
        };

        foreach (var role in model.RoleIds)
            claims.Add(new Claim("role", role.ToString()));

        // Per-user security stamp — lets the API revoke outstanding access tokens
        // (validated per request when Authentication:ValidateAccessTokenStamp is on).
        if (!string.IsNullOrEmpty(model.SecurityStamp))
            claims.Add(new Claim("sstamp", model.SecurityStamp));

        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer:             _options.Issuer,
            audience:           _options.Audience,
            claims:             claims,
            expires:            expiresAt,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token)
    {
        try
        {
            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwt) return null;
            if (!jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase)) return null;

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
