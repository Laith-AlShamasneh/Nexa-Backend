using System.Net;
using System.Net.Mail;
using Application.Interfaces.Services;
using Infrastructure.Services.Email.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services.Email;

internal sealed class SmtpEmailService(
    IOptions<SmtpOptions>      options,
    ILogger<SmtpEmailService>  logger) : IEmailService
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(
        string                             to,
        string                             subject,
        string                             htmlBody,
        IReadOnlyList<EmailAttachmentData>? attachments = null,
        CancellationToken                  ct = default)
    {
        using var client = new SmtpClient(_options.Host, _options.Port)
        {
            Credentials       = new NetworkCredential(_options.Username, _options.Password),
            EnableSsl         = _options.UseSsl,
            DeliveryMethod    = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = false
        };

        using var message = new MailMessage
        {
            From       = new MailAddress(_options.FromAddress, _options.FromName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true
        };
        message.To.Add(to);

        if (attachments is not null)
        {
            foreach (var att in attachments)
            {
                var stream = new MemoryStream(att.Content);
                message.Attachments.Add(new Attachment(stream, att.FileName, att.ContentType));
            }
        }

        try
        {
            await client.SendMailAsync(message, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send email to {To}.", to);
            throw;
        }
    }
}
