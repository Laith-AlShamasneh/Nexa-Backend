namespace Application.Interfaces.Services;

public sealed record EmailAttachmentData(
    string FileName,
    byte[] Content,
    string ContentType);

public interface IEmailService
{
    /// <summary>
    /// <paramref name="plainTextBody"/> is optional but should be supplied whenever
    /// available (see <see cref="IEmailTemplateService.RenderAsync"/>) — implementations
    /// send it as the multipart/alternative text/plain part alongside the HTML body,
    /// not as a fallback that replaces it.
    /// </summary>
    Task SendAsync(
        string                          to,
        string                          subject,
        string                          htmlBody,
        string?                         plainTextBody = null,
        IReadOnlyList<EmailAttachmentData>? attachments = null,
        CancellationToken               ct = default);
}
