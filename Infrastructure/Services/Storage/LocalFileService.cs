using Application.Interfaces.Services;
using Infrastructure.Services.Storage.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Storage;

/// <summary>
/// Disk-backed <see cref="IFileService"/>. Writes are streamed straight through to a
/// temp file (never buffered in memory) and only become visible at their final path
/// via an atomic rename — a reader can never observe a partially-written file, and a
/// cancelled or failed upload never leaves a corrupt file at the destination key.
/// </summary>
internal sealed class LocalFileService(
    IHostEnvironment environment,
    IOptions<StorageOptions> options) : IFileService
{
    private readonly string _uploadsRoot =
        Path.GetFullPath(Path.Combine(environment.ContentRootPath, "wwwroot", "uploads"));

    private int BufferSize => options.Value.CopyBufferSizeBytes;

    public async Task UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken ct = default,
        long? expectedLength = null)
    {
        var path = GetPhysicalPath(key);
        var directory = Path.GetDirectoryName(path)!;
        Directory.CreateDirectory(directory);

        // Written in the same directory as the destination so the final File.Move is
        // a same-volume rename — atomic on both Windows and Linux, not a copy.
        var tempPath = Path.Combine(directory, $".upload-{Guid.NewGuid():N}.tmp");

        try
        {
            await using (var destination = new FileStream(tempPath, new FileStreamOptions
            {
                Mode       = FileMode.CreateNew,
                Access     = FileAccess.Write,
                Share      = FileShare.None,
                Options    = FileOptions.Asynchronous | FileOptions.SequentialScan,
                BufferSize = BufferSize
            }))
            {
                // Pre-size the file when the caller knows the length up front (e.g.
                // IFormFile.Length). Reduces fragmentation for large files and fails
                // fast on insufficient disk space instead of discovering it mid-copy.
                if (expectedLength is > 0)
                    destination.SetLength(expectedLength.Value);

                await stream.CopyToAsync(destination, BufferSize, ct);
            }

            File.Move(tempPath, path, overwrite: true);
        }
        catch
        {
            TryDeletePhysical(tempPath);
            throw;
        }
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        TryDeletePhysical(GetPhysicalPath(key));
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken ct = default) =>
        Task.FromResult(File.Exists(GetPhysicalPath(key)));

    public Task<Stream> DownloadAsync(string key, CancellationToken ct = default)
    {
        Stream stream = new FileStream(GetPhysicalPath(key), new FileStreamOptions
        {
            Mode       = FileMode.Open,
            Access     = FileAccess.Read,
            Share      = FileShare.Read,
            Options    = FileOptions.Asynchronous | FileOptions.SequentialScan,
            BufferSize = BufferSize
        });
        return Task.FromResult(stream);
    }

    private string GetPhysicalPath(string key)
    {
        var relative = key.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_uploadsRoot, relative));

        // Defense in depth: reject any key that escapes the uploads root (e.g. "..\").
        if (!fullPath.Equals(_uploadsRoot, StringComparison.OrdinalIgnoreCase)
            && !fullPath.StartsWith(_uploadsRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Resolved file path escapes the uploads root.");

        return fullPath;
    }

    private static void TryDeletePhysical(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (IOException)
        {
            // Best-effort cleanup — a locked/in-use temp file on Windows shouldn't
            // fail the caller's already-decided outcome (upload success/failure).
        }
    }
}
