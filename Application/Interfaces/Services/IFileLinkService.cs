using Shared.Enums.System;

namespace Application.Interfaces.Services;

/// <summary>
/// Mints and validates short-lived, signed "capability" URLs for private files
/// (receipts, etc.). A signed link is only ever created for a caller who was
/// authorized to see the file; the HMAC signature makes the link unforgeable and
/// the embedded expiry makes it self-revoking. This lets the browser load images
/// via a plain URL without exposing the file to anonymous static-file access.
/// </summary>
public interface IFileLinkService
{
    string CreateSignedFileUrl(string baseUrl, FolderPaths folder, string fileName, TimeSpan? ttl = null);

    bool TryValidate(string key, long expiresAtUnix, string signature);
}
