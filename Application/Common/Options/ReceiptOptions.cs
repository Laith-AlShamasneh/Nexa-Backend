namespace Application.Common.Options;

public sealed class ReceiptOptions
{
    public long    MaxFileSizeBytes   { get; init; } = 10_485_760;  // 10 MB
    public int     OcrBatchSize       { get; init; } = 5;
    public string[] AllowedExtensions { get; init; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".webp", ".pdf", ".heic"
    ];
}
