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
}
