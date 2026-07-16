namespace Shared.Enums.System;

/// <summary>
/// Centralizes the language/direction/code mapping that was previously duplicated
/// ad hoc (<c>isArabic ? "ar" : "en"</c>) across the email template service and
/// individual job handlers. Any code that needs to answer "is this RTL?" or "what's
/// the two-letter code?" for a <see cref="SystemLanguages"/> value should use this,
/// not re-derive it locally — that duplication is exactly how the two mechanisms
/// (message-key JSON catalog vs. email template files) drifted apart in the first
/// place.
/// </summary>
public static class SystemLanguageExtensions
{
    public static bool IsRightToLeft(this SystemLanguages language) =>
        language == SystemLanguages.Arabic;

    /// <summary>Two-letter code used for file-naming conventions ("en"/"ar") and HTML lang attributes.</summary>
    public static string ToLanguageCode(this SystemLanguages language) =>
        language switch
        {
            SystemLanguages.Arabic  => "ar",
            SystemLanguages.English => "en",
            _                        => "en"
        };

    /// <summary>"rtl"/"ltr" — the HTML <c>dir</c> attribute value.</summary>
    public static string ToDirection(this SystemLanguages language) =>
        language.IsRightToLeft() ? "rtl" : "ltr";

    public static SystemLanguages FromLanguageCode(string? code) =>
        string.Equals(code, "ar", StringComparison.OrdinalIgnoreCase)
            ? SystemLanguages.Arabic
            : SystemLanguages.English;
}
