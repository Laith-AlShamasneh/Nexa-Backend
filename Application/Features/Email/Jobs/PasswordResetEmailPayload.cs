using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record PasswordResetEmailPayload(
    string          RecipientEmail,
    string          DisplayName,
    string          ResetLink,
    SystemLanguages Language);
