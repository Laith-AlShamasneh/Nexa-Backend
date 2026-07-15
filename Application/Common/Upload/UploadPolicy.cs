namespace Application.Common.Upload;

public sealed record UploadPolicy(
    string[] AllowedMimeTypes,
    string[] AllowedExtensions,
    long MaxSizeBytes);

public static class UploadPolicies
{
    // Profile pictures: JPEG and PNG only; no WebP, SVG, PDF, or executables.
    public static readonly UploadPolicy ProfileImage = new(
        AllowedMimeTypes:  ["image/jpeg", "image/png"],
        AllowedExtensions: [".jpg", ".jpeg", ".png"],
        MaxSizeBytes:      5 * 1024 * 1024);

    // Organization logos: also allow WebP (transparency, smaller size — common for
    // logos) alongside JPEG/PNG. No SVG: an SVG can embed <script>/event-handler
    // content and is rendered directly by browsers, making it an XSS vector if ever
    // served inline instead of downloaded.
    public static readonly UploadPolicy OrganizationLogo = new(
        AllowedMimeTypes:  ["image/jpeg", "image/png", "image/webp"],
        AllowedExtensions: [".jpg", ".jpeg", ".png", ".webp"],
        MaxSizeBytes:      5 * 1024 * 1024);
}
