using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record WelcomeEmailPayload(
    string          RecipientEmail,
    string          DisplayName,
    SystemLanguages Language);
