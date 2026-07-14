using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record EmailChangedPayload(
    string          RecipientEmail,
    string          DisplayName,
    string          NewEmail,
    string          ChangeTime,
    SystemLanguages Language
);
