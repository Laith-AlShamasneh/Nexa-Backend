namespace Application.Interfaces.Services;

public interface IFileService
{
    Task UploadAsync(Stream stream, string key, string contentType, CancellationToken ct = default);
    Task DeleteAsync(string key, CancellationToken ct = default);
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    Task<Stream> DownloadAsync(string key, CancellationToken ct = default);
}
