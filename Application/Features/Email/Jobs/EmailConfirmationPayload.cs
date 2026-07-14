using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record EmailConfirmationPayload(
    string          RecipientEmail,
    string          DisplayName,
    string          ConfirmationLink,
    SystemLanguages Language);
