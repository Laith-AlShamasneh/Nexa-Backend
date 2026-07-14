using System.Text.Json;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Shared.Enums.System;

namespace Infrastructure.Services.Email;

/// <summary>
/// Loads email templates from physical HTML files under WebApi/EmailTemplates/.
///
/// Directory structure:
///   EmailTemplates/
///   ├── Layouts/
///   │   ├── base-en.html          ← full page wrapper; injects {{Content}}
///   │   └── base-ar.html
///   ├── {TemplateKey}/
///   │   ├── meta.json             ← { SubjectEn, SubjectAr, PreviewEn, PreviewAr }
///   │   ├── en.html               ← inner body content
///   │   └── ar.html
///   └── ...
///
/// Placeholder syntax: {{PlaceholderName}}
/// Reserved layout placeholders: {{Content}}, {{Subject}}, {{PreviewText}}, {{CurrentYear}}
/// </summary>
internal sealed class EmailTemplateService(
    IWebHostEnvironment environment,
    IMemoryCache        cache) : IEmailTemplateService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<(string Subject, string HtmlBody)> RenderAsync(
        string                     templateKey,
        SystemLanguages            language,
        Dictionary<string, string> placeholders,
        CancellationToken          ct = default)
    {
        var isArabic = language == SystemLanguages.Arabic;
        var lang     = isArabic ? "ar" : "en";

        // 1. Load metadata (subjects + preview text)
        var meta = await LoadMetaAsync(templateKey, ct);

        var subject     = isArabic ? meta.SubjectAr  : meta.SubjectEn;
        var previewText = isArabic ? meta.PreviewAr  : meta.PreviewEn;

        // 2. Load base layout and content fragment
        var layout  = await LoadFileAsync($"Layouts/base-{lang}.html",  ct);
        var content = await LoadFileAsync($"{templateKey}/{lang}.html",  ct);

        // 3. Apply feature-specific placeholders to the content fragment
        content = ApplyPlaceholders(content, placeholders);

        // 4. Build full set of layout-level placeholders and compose
        var layoutPlaceholders = new Dictionary<string, string>(placeholders, StringComparer.OrdinalIgnoreCase)
        {
            ["Content"]     = content,
            ["Subject"]     = ApplyPlaceholders(subject, placeholders),
            ["PreviewText"] = ApplyPlaceholders(previewText, placeholders),
            ["CurrentYear"] = DateTime.UtcNow.Year.ToString()
        };

        // Apply subject placeholder substitution after resolving feature placeholders
        subject = ApplyPlaceholders(subject, placeholders);

        var html = ApplyPlaceholders(layout, layoutPlaceholders);

        return (subject, html);
    }

    // ─── File loading with 12-hour cache ────────────────────────────────────

    private async Task<string> LoadFileAsync(string relativePath, CancellationToken ct)
    {
        if (cache.TryGetValue(relativePath, out string? cached) && cached is not null)
            return cached;

        var fullPath = Path.Combine(environment.ContentRootPath, "EmailTemplates",
            relativePath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Email template file not found: {relativePath}", fullPath);

        var content = await File.ReadAllTextAsync(fullPath, ct);
        cache.Set(relativePath, content, TimeSpan.FromHours(12));
        return content;
    }

    private async Task<TemplateMeta> LoadMetaAsync(string templateKey, CancellationToken ct)
    {
        var cacheKey = $"email-meta:{templateKey}";
        if (cache.TryGetValue(cacheKey, out TemplateMeta? cached) && cached is not null)
            return cached;

        var fullPath = Path.Combine(environment.ContentRootPath, "EmailTemplates",
            templateKey, "meta.json");

        if (!File.Exists(fullPath))
            throw new FileNotFoundException($"Email template metadata not found for '{templateKey}'.", fullPath);

        await using var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read,
                                                FileShare.Read, 4096, useAsync: true);
        var meta = await JsonSerializer.DeserializeAsync<TemplateMeta>(stream, _jsonOptions, ct)
            ?? throw new InvalidOperationException($"Failed to deserialize meta.json for template '{templateKey}'.");

        cache.Set(cacheKey, meta, TimeSpan.FromHours(12));
        return meta;
    }

    // ─── Placeholder engine ─────────────────────────────────────────────────

    private static string ApplyPlaceholders(string template, Dictionary<string, string> placeholders)
    {
        foreach (var (key, value) in placeholders)
            template = template.Replace($"{{{{{key}}}}}", value ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        return template;
    }

    // ─── Internal model ─────────────────────────────────────────────────────

    private sealed record TemplateMeta(
        string SubjectEn,
        string SubjectAr,
        string PreviewEn,
        string PreviewAr);
}
