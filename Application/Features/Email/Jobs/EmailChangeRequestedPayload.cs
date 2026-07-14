using Shared.Enums.System;

namespace Application.Features.Email.Jobs;

public sealed record EmailChangeRequestedPayload(
    string          RecipientEmail,
    string          DisplayName,
    string          ConfirmationLink,
    string          OldEmail,
    SystemLanguages Language
);
