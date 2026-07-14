using Application.Interfaces.Services;
using Microsoft.Extensions.Hosting;
using Shared.Enums.System;
using System.Text.Json;

namespace Infrastructure.Services.Localization;

// Reads two JSON files from wwwroot/resources/ and caches them in memory.
//
// system-messages.json structure:
// { "ar": { "Common.Success": "تم بنجاح", ... }, "en": { "Common.Success": "Success", ... } }
//
// system-labels.json structure:
// { "ar": { "Profile.GetAvatarCategories": { "pageLabel": "الأنماط", ... } }, "en": { ... } }

internal sealed class MessageProvider(
    IUserContext userContext,
    ICacheService cacheService,
    IHostEnvironment environment) : IMessageProvider
{
    private const string MessagesCacheKey = "LOCALIZATION:MESSAGES";
    private const string LabelsCacheKey   = "LOCALIZATION:LABELS";

    public async Task<string> GetMessagesAsync(string key, CancellationToken ct = default)
    {
        var messages = await cacheService
            .GetAsync<Dictionary<string, Dictionary<string, string>>>(MessagesCacheKey);

        if (messages is null)
        {
            var path = Path.Combine(environment.ContentRootPath, "wwwroot", "resources", "system-messages.json");
            if (!File.Exists(path)) return key;

            var json = await File.ReadAllTextAsync(path, ct);
            messages = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

            if (messages is not null)
                await cacheService.SetAsync(MessagesCacheKey, messages);
        }

        if (messages is null) return key;

        var lang = ResolveLanguageKey();

        return messages.TryGetValue(lang, out var section) && section.TryGetValue(key, out var message)
            ? message
            : key;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetLabelsAsync(string section, CancellationToken ct = default)
    {
        var labels = await cacheService
            .GetAsync<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(LabelsCacheKey);

        if (labels is null)
        {
            var path = Path.Combine(environment.ContentRootPath, "wwwroot", "resources", "system-labels.json");
            if (!File.Exists(path)) return new Dictionary<string, string>();

            var json = await File.ReadAllTextAsync(path, ct);
            labels = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, string>>>>(json);

            if (labels is not null)
                await cacheService.SetAsync(LabelsCacheKey, labels);
        }

        if (labels is null) return new Dictionary<string, string>();

        var lang = ResolveLanguageKey();

        return labels.TryGetValue(lang, out var langSection)
            && langSection.TryGetValue(section, out var sectionLabels)
                ? sectionLabels
                : [];
    }

    private string ResolveLanguageKey() =>
        userContext.Language == SystemLanguages.English ? "en" : "ar";
}
