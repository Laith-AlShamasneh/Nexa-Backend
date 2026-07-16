using System.Net;
using System.Text.Json;
using Application.Interfaces.Services;
using Infrastructure.Services.Email.Options;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Shared.Enums.System;

namespace Infrastructure.Services.Email;

/// <summary>
/// Loads and composes email templates from physical files under WebApi/EmailTemplates/.
/// Full design rationale, placeholder reference, and "how to add a template" guide:
/// docs/EMAIL_TEMPLATES.md — read that before changing this file's file-layout
/// convention, since every template on disk depends on it.
///
/// Directory structure:
///   EmailTemplates/
///   ├── Layouts/
///   │   ├── base-en.html / base-ar.html   ← shared chrome (header/card/button/footer)
///   │   └── base-en.txt / base-ar.txt     ← plain-text equivalent
///   ├── {TemplateKey}/
///   │   ├── meta.json                     ← static per-language copy (see TemplateMeta)
///   │   ├── {lang}-body.html              ← required: intro/explanation
///   │   ├── {lang}-secondary.html         ← optional: fallback link, expiry, security note
///   │   ├── {lang}-body.txt
///   │   └── {lang}-secondary.txt          ← optional
///   └── ...
///
/// Placeholder syntax: {{PlaceholderName}} (case-insensitive, no engine — linear
/// string replace, so there is no conditional/loop syntax; every layout placeholder
/// must resolve to a value, even if empty).
///
/// Two-phase substitution, mirroring the trust boundary between "our own copy" and
/// "caller-supplied data":
///   1. The caller's <paramref name="placeholders"/> (dynamic data: names, links,
///      dates) are HTML-encoded and substituted into the {lang}-body/{lang}-secondary
///      fragments. This is the only phase that touches potentially-attacker-influenced
///      strings (e.g. a Person's display name), so it's the only phase that escapes.
///   2. The *results* of phase 1 (already-safe, already-rendered HTML) plus this
///      service's own static values (Title, Subject, PrimaryButtonText, FooterContent,
///      CurrentYear, CompanyName from <see cref="EmailBrandingOptions"/>) are merged
///      into the layout. Nothing here is escaped — these are all either our own
///      authored HTML or phase-1 output that's already safe.
/// </summary>
internal sealed class EmailTemplateService(
    IHostEnvironment                  environment,
    IMemoryCache                      cache,
    IOptions<EmailBrandingOptions>    brandingOptions) : IEmailTemplateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(12);

    private readonly EmailBrandingOptions _branding = brandingOptions.Value;

    public async Task<(string Subject, string HtmlBody, string PlainTextBody)> RenderAsync(
        string                     templateKey,
        SystemLanguages            language,
        Dictionary<string, string> placeholders,
        CancellationToken          ct = default)
    {
        var lang = language.ToLanguageCode();
        var meta = await LoadMetaAsync(templateKey, ct);

        // CompanyName must be available before subject/preheader are interpolated —
        // meta.json copy (e.g. "Confirm your {{CompanyName}} account") can reference
        // it just like any per-send placeholder.
        var withBranding = new Dictionary<string, string>(placeholders, StringComparer.OrdinalIgnoreCase)
        {
            ["CompanyName"] = _branding.CompanyName
        };

        var title      = Pick(language, meta.TitleEn, meta.TitleAr);
        var buttonText = Pick(language, meta.ButtonTextEn, meta.ButtonTextAr);
        var subject    = ApplyPlaceholders(Pick(language, meta.SubjectEn, meta.SubjectAr), withBranding, escape: false);
        var preheader  = ApplyPlaceholders(Pick(language, meta.PreviewEn, meta.PreviewAr), withBranding, escape: false);

        var html = await RenderVariantAsync(
            templateKey, lang, "html", title, buttonText, subject, preheader, withBranding, ct);

        var text = await RenderVariantAsync(
            templateKey, lang, "txt", title, buttonText, subject, preheader: null, withBranding, ct);

        return (subject, html, text);
    }

    // ─── Composition (shared between the HTML and plain-text variants) ─────────

    private async Task<string> RenderVariantAsync(
        string                     templateKey,
        string                     lang,
        string                     extension,
        string                     title,
        string                     buttonText,
        string                     subject,
        string?                    preheader,
        Dictionary<string, string> placeholders,
        CancellationToken          ct)
    {
        var isHtml = extension == "html";

        // LoadFileAsync's return type is uniformly nullable (optional files can be
        // absent), but required: true guarantees a FileNotFoundException instead of
        // null — the ! is safe here, not a suppressed real nullability risk.
        var layout    = (await LoadFileAsync($"Layouts/base-{lang}.{extension}", required: true, ct))!;
        var body      = (await LoadFileAsync($"{templateKey}/{lang}-body.{extension}", required: true, ct))!;
        var secondary = await LoadFileAsync($"{templateKey}/{lang}-secondary.{extension}", required: false, ct);

        body      = ApplyPlaceholders(body ?? string.Empty, placeholders, escape: isHtml);
        secondary = ApplyPlaceholders(secondary ?? string.Empty, placeholders, escape: isHtml);

        var layoutPlaceholders = new Dictionary<string, string>(placeholders, StringComparer.OrdinalIgnoreCase)
        {
            ["Title"]             = title,
            ["Subject"]           = subject,
            ["Preheader"]         = preheader ?? string.Empty,
            ["BodyContent"]       = body,
            ["SecondaryContent"]  = secondary,
            ["PrimaryButtonText"] = buttonText,
            ["FooterContent"]     = BuildFooterContent(lang, isHtml),
            ["CompanyName"]       = _branding.CompanyName,
            ["CurrentYear"]       = DateTime.UtcNow.Year.ToString()
            // PrimaryButtonUrl is intentionally left to the caller's placeholders —
            // it's per-send dynamic data (a confirmation link, a reset link, ...),
            // never something this service can know generically.
        };

        return ApplyPlaceholders(layout, layoutPlaceholders, escape: false);
    }

    private string BuildFooterContent(string lang, bool isHtml)
    {
        if (string.IsNullOrWhiteSpace(_branding.SupportEmail))
            return string.Empty;

        if (isHtml)
        {
            var label = lang == "ar" ? "بحاجة إلى مساعدة؟ راسلنا على" : "Need help? Contact us at";
            var email = WebUtility.HtmlEncode(_branding.SupportEmail);
            return $"{label} <a href=\"mailto:{email}\" style=\"color:#4F46E5;text-decoration:none;\">{email}</a>.";
        }

        var textLabel = lang == "ar" ? "بحاجة إلى مساعدة؟ راسلنا على" : "Need help? Contact us at";
        return $"{textLabel} {_branding.SupportEmail}.";
    }

    private static string Pick(SystemLanguages language, string en, string ar) =>
        language.IsRightToLeft() ? ar : en;

    // ─── File loading with 12-hour cache ────────────────────────────────────

    private async Task<string?> LoadFileAsync(string relativePath, bool required, CancellationToken ct)
    {
        var cacheKey = $"email-file:{relativePath}";
        if (cache.TryGetValue(cacheKey, out string? cached))
            return cached;

        var fullPath = Path.Combine(environment.ContentRootPath, "EmailTemplates",
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
        {
            if (required)
                throw new FileNotFoundException($"Email template file not found: {relativePath}", fullPath);

            // Optional files (the secondary-content section) legitimately don't exist
            // for every template — cache the "absent" result too so a missing optional
            // file doesn't cost a disk stat on every render.
            cache.Set<string?>(cacheKey, null, CacheTtl);
            return null;
        }

        var content = await File.ReadAllTextAsync(fullPath, ct);
        cache.Set(cacheKey, content, CacheTtl);
        return content;
    }

    private async Task<TemplateMeta> LoadMetaAsync(string templateKey, CancellationToken ct)
    {
        var cacheKey = $"email-meta:{templateKey}";
        if (cache.TryGetValue(cacheKey, out TemplateMeta? cached) && cached is not null)
            return cached;

        var fullPath = Path.Combine(environment.ContentRootPath, "EmailTemplates", templateKey, "meta.json");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Email template metadata not found for '{templateKey}'.", fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read,
            FileShare.Read, 4096, useAsync: true);
        var meta = await JsonSerializer.DeserializeAsync<TemplateMeta>(stream, JsonOptions, ct)
            ?? throw new InvalidOperationException($"Failed to deserialize meta.json for template '{templateKey}'.");

        cache.Set(cacheKey, meta, CacheTtl);
        return meta;
    }

    // ─── Placeholder engine ─────────────────────────────────────────────────

    private static string ApplyPlaceholders(string template, Dictionary<string, string> placeholders, bool escape)
    {
        foreach (var (key, value) in placeholders)
        {
            var safeValue = escape ? WebUtility.HtmlEncode(value ?? string.Empty) : (value ?? string.Empty);
            template = template.Replace($"{{{{{key}}}}}", safeValue, StringComparison.OrdinalIgnoreCase);
        }
        return template;
    }

    // ─── Internal model ─────────────────────────────────────────────────────

    /// <summary>
    /// Static, translatable, per-template copy that never varies per send — as
    /// opposed to the caller's <c>placeholders</c> dictionary, which is per-send
    /// dynamic data (names, links, dates). Keeping these separate means adding a
    /// template only requires editing files, never touching handler code for the
    /// wording itself.
    /// </summary>
    private sealed record TemplateMeta(
        string SubjectEn, string SubjectAr,
        string PreviewEn, string PreviewAr,
        string TitleEn, string TitleAr,
        string ButtonTextEn, string ButtonTextAr);
}
