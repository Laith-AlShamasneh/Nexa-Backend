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
}
