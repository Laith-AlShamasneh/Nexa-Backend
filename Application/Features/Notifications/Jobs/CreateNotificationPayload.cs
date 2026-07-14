namespace Application.Features.Notifications.Jobs;

public sealed record CreateNotificationPayload(
    string                       TemplateCode,
    long                         UserId,
    Dictionary<string, string>?  Parameters,
    string?                      PayloadJson,
    // Optional pre-rendered text. When supplied, the handler uses these verbatim
    // instead of applying {placeholder} substitution to the template translations
    // (for features that already compute the final bilingual text, e.g. cash-flow
    // forecast risks). The template is still resolved for category/type/priority.
    string?                      TitleEn   = null,
    string?                      TitleAr   = null,
    string?                      MessageEn = null,
    string?                      MessageAr = null);
