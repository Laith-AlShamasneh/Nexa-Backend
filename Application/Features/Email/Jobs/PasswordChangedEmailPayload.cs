using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record PasswordChangedEmailPayload(
    string          RecipientEmail,
    string          DisplayName,
    string          ChangeTime,
    SystemLanguages Language);
