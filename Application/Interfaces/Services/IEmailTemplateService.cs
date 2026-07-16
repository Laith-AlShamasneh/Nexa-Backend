using Shared.Enums.System;

namespace Application.Interfaces.Services;

public interface IEmailTemplateService
{
    /// <summary>
    /// Renders a named template (see docs/EMAIL_TEMPLATES.md) for the given language
    /// against the shared base layout, returning the subject, HTML body, and a plain-
    /// text alternative — all three should be sent together
    /// (<see cref="IEmailService.SendAsync"/>'s <c>plainTextBody</c> parameter) so mail
    /// clients that prefer text, and spam filters that penalize HTML-only mail, both
    /// get a proper multipart/alternative message.
    /// </summary>
    Task<(string Subject, string HtmlBody, string PlainTextBody)> RenderAsync(
        string                       templateKey,
        SystemLanguages              language,
        Dictionary<string, string>   placeholders,
        CancellationToken            ct = default);
}
