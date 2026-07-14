using Application.Interfaces.Services;
using Infrastructure.Services.Storage.Options;
using Microsoft.Extensions.Options;
using Shared.Enums.System;

namespace Infrastructure.Services.Storage;

internal sealed class StorageUtility(IOptions<StorageOptions> options) : IStorageUtility
{
    private readonly StorageOptions _options = options.Value;

    public (string FullPath, TimeSpan Expiration) BuildFilePathWithExpiration(
        FolderPaths folderPathName,
        string fileKey,
        bool isInternalStorage = false,
        string? baseUrl = null,
        int? duration = null,
        TimeUnits? timeUnit = null)
    {
        _options.FolderPaths.TryGetValue(folderPathName.ToString(), out var basePath);

        // Internal storage: build a public URL using the provided baseUrl (no signed expiry needed).
        // Files are served from wwwroot/uploads/{folder}/{file}, so the URL is always
        // {baseUrl}/uploads/{configuredFolderPath}/{fileName}.
        if (isInternalStorage)
        {
            var safeBase = baseUrl?.TrimEnd('/') ?? string.Empty;
            var safeDir  = basePath?.Trim('/') ?? string.Empty;
            var safeKey  = fileKey.TrimStart('/');

            var fullPath = string.IsNullOrWhiteSpace(safeDir)
                ? $"{safeBase}/uploads/{safeKey}"
                : $"{safeBase}/uploads/{safeDir}/{safeKey}";

            return (fullPath, TimeSpan.MaxValue);
        }

        // External storage: build a relative key for pre-signed URL generation
        var path = string.IsNullOrWhiteSpace(basePath)
            ? fileKey.TrimStart('/')
            : $"{basePath.TrimEnd('/')}/{fileKey.TrimStart('/')}";

        var expiration = duration.HasValue && timeUnit.HasValue
            ? BuildExpiration(duration.Value, timeUnit.Value)
            : TimeSpan.FromDays(1);

        return (path, expiration);
    }

    public string BuildFileKey(FolderPaths folder, string fileName)
    {
        _options.FolderPaths.TryGetValue(folder.ToString(), out var folderPath);
        var safeFolder = folderPath?.Trim('/') ?? string.Empty;
        return string.IsNullOrWhiteSpace(safeFolder)
            ? fileName.TrimStart('/')
            : $"{safeFolder}/{fileName.TrimStart('/')}";
    }

    public TimeSpan BuildExpiration(int duration, TimeUnits timeUnit) => timeUnit switch
    {
        TimeUnits.Seconds => TimeSpan.FromSeconds(duration),
        TimeUnits.Minutes => TimeSpan.FromMinutes(duration),
        TimeUnits.Hours   => TimeSpan.FromHours(duration),
        TimeUnits.Days    => TimeSpan.FromDays(duration),
        TimeUnits.Weeks   => TimeSpan.FromDays(duration * 7),
        TimeUnits.Months  => TimeSpan.FromDays(duration * 30),
        TimeUnits.Years   => TimeSpan.FromDays(duration * 365),
        _                 => TimeSpan.FromDays(duration)
    };
}
