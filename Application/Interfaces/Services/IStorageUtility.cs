using Shared.Enums.System;

namespace Application.Interfaces.Services;

public interface IStorageUtility
{
    (string FullPath, TimeSpan Expiration) BuildFilePathWithExpiration(
        FolderPaths folderPathName,
        string fileKey,
        bool isInternalStorage = false,
        string? baseUrl = null,
        int? duration = null,
        TimeUnits? timeUnit = null);

    TimeSpan BuildExpiration(int duration, TimeUnits timeUnit);

    string BuildFileKey(FolderPaths folder, string fileName);
}
