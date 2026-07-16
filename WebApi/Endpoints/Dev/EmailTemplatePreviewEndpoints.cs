using System.Net;
using Application.Common.Constants;
using Application.Interfaces.Services;
using Shared.Enums.System;

namespace WebApi.Endpoints.Dev;

/// <summary>
/// Development-only preview surface for email templates. Renders a template with
/// realistic sample data and returns the raw HTML or plain text, so a template can
/// be eyeballed in a browser — or saved to a file via the browser's "Save As", or
/// <c>curl ... > preview.html</c> — without ever going through <see cref="IEmailService"/>
/// or sending a real email. Only mapped when the host environment is Development
/// (see the call site in WebApi/Program.cs) — never reachable in Staging/Production.
///
/// Usage: GET /dev/email-templates/{templateKey}?lang=en|ar&amp;format=html|text
/// Example: /dev/email-templates/EmailConfirmation?lang=ar&amp;format=html
/// </summary>
public static class EmailTemplatePreviewEndpoints
{
    public static IEndpointRouteBuilder MapEmailTemplatePreviewEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/dev/email-templates/{templateKey}", PreviewAsync)
            .WithName("PreviewEmailTemplate")
            .WithTags("Dev - Email Template Previews")
            .ExcludeFromDescription(); // dev-only tooling, not part of the public API surface

        return app;
    }

    private static async Task<IResult> PreviewAsync(
        string                 templateKey,
        IEmailTemplateService  templateService,
        string                 lang = "en",
        string                 format = "html",
        CancellationToken      ct = default)
    {
        var language      = SystemLanguageExtensions.FromLanguageCode(lang);
        var placeholders  = EmailPreviewSampleData.For(templateKey);

        var (subject, htmlBody, plainTextBody) = await templateService.RenderAsync(templateKey, language, placeholders, ct);

        if (string.Equals(format, "text", StringComparison.OrdinalIgnoreCase))
            return Results.Text($"Subject: {subject}\r\n\r\n{plainTextBody}", "text/plain; charset=utf-8");

        // Surface the subject line above the rendered body — useful for eyeballing
        // in a browser, harmless to strip out before saving as a real .html file.
        var withSubjectBanner =
            $"<!-- Subject: {WebUtility.HtmlEncode(subject)} -->\n{htmlBody}";

        return Results.Content(withSubjectBanner, "text/html; charset=utf-8");
    }
}

/// <summary>
/// Realistic sample data per template key, used only by the dev preview endpoint —
/// never sent in a real email. When adding a new template, add its sample data here
/// so the new template is previewable immediately (see docs/EMAIL_TEMPLATES.md "How
/// to add a new template").
/// </summary>
internal static class EmailPreviewSampleData
{
    public static Dictionary<string, string> For(string templateKey) => templateKey switch
    {
        JobTypes.EmailConfirmation => new Dictionary<string, string>
        {
            ["DisplayName"]      = "Laith Al-Shamasneh",
            ["ConfirmationLink"] = "https://app.nexa.local/confirm-email?token=preview-sample-token",
            ["PrimaryButtonUrl"] = "https://app.nexa.local/confirm-email?token=preview-sample-token"
        },

        // Fallback for any template key that doesn't have curated sample data yet —
        // enough to render without a NullReferenceException, not a real preview.
        _ => new Dictionary<string, string>
        {
            ["DisplayName"]      = "Sample User",
            ["PrimaryButtonUrl"] = "https://example.com"
        }
    };
}
