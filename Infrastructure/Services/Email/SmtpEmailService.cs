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
        string?                            plainTextBody = null,
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
            From = new MailAddress(_options.FromAddress, _options.FromName),
            Subject = subject
        };
        message.To.Add(to);

        // A real multipart/alternative message (text/plain + text/html), not just an
        // HTML body with no fallback — mail clients that prefer plain text render the
        // text part, and HTML-only mail is a well-known spam-score penalty. When no
        // plain-text alternative is supplied, fall back to the previous HTML-only body
        // rather than forcing every caller to provide one.
        if (plainTextBody is not null)
        {
            message.Body = plainTextBody;
            message.IsBodyHtml = false;
            message.AlternateViews.Add(
                AlternateView.CreateAlternateViewFromString(htmlBody, null, "text/html"));
        }
        else
        {
            message.Body = htmlBody;
            message.IsBodyHtml = true;
        }

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
