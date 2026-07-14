using System.Text.Json;
using Shared.Enums.System;

namespace Shared.Utilities;

public static class LocalizationUtility
{
    public static string GetLocalizedText(string jsonValue, SystemLanguages systemLanguages)
    {
        if (string.IsNullOrWhiteSpace(jsonValue))
            return string.Empty;

        try
        {
            var labels = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonValue);
            if (labels is null)
                return string.Empty;

            var langKey = systemLanguages == SystemLanguages.English ? "en" : "ar";

            return labels.TryGetValue(langKey, out var val)
                ? val
                : labels.GetValueOrDefault("ar", string.Empty);
        }
        catch
        {
            return string.Empty;
        }
    }
}