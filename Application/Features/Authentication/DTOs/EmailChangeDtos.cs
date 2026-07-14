namespace Application.Features.Authentication.DTOs;

public sealed record RequestEmailChangeRequest(
    string NewEmail,
    string CurrentPassword
);

public sealed record ConfirmEmailChangeRequest(string Token);
