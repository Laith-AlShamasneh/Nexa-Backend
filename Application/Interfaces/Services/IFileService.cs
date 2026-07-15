namespace Application.Interfaces.Services;

public interface IFileService
{
    /// <summary>
    /// Streams <paramref name="stream"/> to storage under <paramref name="key"/>.
    /// Implementations must never buffer the whole file in memory — callers may
    /// pass multi-gigabyte streams. <paramref name="expectedLength"/>, when known
    /// (e.g. from <c>IFormFile.Length</c>), lets the implementation pre-size the
    /// destination to reduce fragmentation and fail fast on insufficient disk space;
    /// it is an optimization hint, not a hard constraint.
    /// </summary>
    Task UploadAsync(
        Stream stream,
        string key,
        string contentType,
        CancellationToken ct = default,
        long? expectedLength = null);

    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Opens a read stream for <paramref name="key"/>. Implementations must return a
    /// stream suitable for direct async streaming to an HTTP response (e.g. via
    /// <c>Results.Stream</c>) without buffering the whole file in memory first.
    /// </summary>
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
}
