namespace Application.Common.Upload;

/// <summary>
/// Transport-agnostic stand-in for an uploaded file. WebApi maps <c>IFormFile</c>
/// (or any other transport) into this at the API boundary — Application must not
/// depend on ASP.NET Core request types.
/// </summary>
public sealed class FileUpload
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long Length { get; init; }
    public required Stream Content { get; init; }
}
