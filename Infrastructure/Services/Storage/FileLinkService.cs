using Application.Interfaces.Services;
using Infrastructure.Services.Authentication.Options;
using Microsoft.Extensions.Options;
using Shared.Enums.System;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Services.Storage;

internal sealed class FileLinkService(
    IStorageUtility       storageUtility,
    IOptions<JwtOptions>  jwtOptions) : IFileLinkService
{
    private static readonly TimeSpan DefaultTtl = TimeSpan.FromHours(1);

    // Derive a purpose-bound signing key from the JWT secret so file links cannot
    // be used to forge tokens and vice-versa, without introducing a second secret.
    private readonly byte[] _signingKey =
        SHA256.HashData(Encoding.UTF8.GetBytes("filelink:" + jwtOptions.Value.SecretKey));

    public string CreateSignedFileUrl(string baseUrl, FolderPaths folder, string fileName, TimeSpan? ttl = null)
    {
        var key = storageUtility.BuildFileKey(folder, fileName);
        var exp = DateTimeOffset.UtcNow.Add(ttl ?? DefaultTtl).ToUnixTimeSeconds();
        var sig = Sign(key, exp);

        var safeBase = baseUrl?.TrimEnd('/') ?? string.Empty;
        return $"{safeBase}/api/files/view?k={Uri.EscapeDataString(key)}&e={exp}&s={sig}";
    }

    public bool TryValidate(string key, long expiresAtUnix, string signature)
    {
        if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(signature))
            return false;

        if (DateTimeOffset.FromUnixTimeSeconds(expiresAtUnix) < DateTimeOffset.UtcNow)
            return false;

        var expected = Sign(key, expiresAtUnix);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(signature));
    }

    private string Sign(string key, long exp)
    {
        using var hmac = new HMACSHA256(_signingKey);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes($"{key}\n{exp}"));
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
