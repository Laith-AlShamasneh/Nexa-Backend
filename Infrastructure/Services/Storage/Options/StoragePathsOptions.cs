namespace Infrastructure.Services.Storage.Options;

// Maps FolderPaths enum names to actual storage folder paths.
// Configured in appsettings.json under "StoragePaths:FolderPaths".
//
// Example appsettings.json:
// "Storage": {
//   "FolderPaths": {
//     "IconsFolder":                        "icons",
//     "AvatarsFolder":                      "avatars",
//     "AvatarCustomizationCategoriesFolder":"avatar-customization-categories"
//   }
// }
public sealed class StorageOptions
{
    public Dictionary<string, string> FolderPaths { get; init; } = [];

    // Buffer size used for both the destination FileStream and the CopyToAsync call
    // in LocalFileService. 1 MiB keeps syscall count low for large uploads/downloads
    // without holding an unreasonable amount of memory per concurrent transfer.
    // Override via "Storage:CopyBufferSizeBytes" for environments that routinely
    // move very large files.
    public int CopyBufferSizeBytes { get; init; } = 1024 * 1024;
}
