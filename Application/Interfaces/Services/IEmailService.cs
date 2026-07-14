namespace Application.Interfaces.Services;

public sealed record EmailAttachmentData(
    string FileName,
    byte[] Content,
    string ContentType);

public interface IEmailService
{
    Task SendAsync(
        string                          to,
        string                          subject,
        string                          htmlBody,
        IReadOnlyList<EmailAttachmentData>? attachments = null,
        CancellationToken               ct = default);
}
