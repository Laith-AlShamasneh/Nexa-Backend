using Application.Interfaces.Services;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.Authentication;

internal sealed class TokenHasher : ITokenHasher
{
    public string GenerateRawToken()
    {
        var bytes = new byte[64];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes);
    }

    public string Hash(string rawToken)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
