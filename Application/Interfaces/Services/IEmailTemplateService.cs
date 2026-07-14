using Shared.Enums.System;

namespace Application.Interfaces.Services;

public interface IEmailTemplateService
{
    Task<(string Subject, string HtmlBody)> RenderAsync(
        string                       templateKey,
        SystemLanguages              language,
        Dictionary<string, string>   placeholders,
        CancellationToken            ct = default);
}
