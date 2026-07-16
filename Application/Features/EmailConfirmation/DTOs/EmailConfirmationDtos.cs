namespace Application.Features.EmailConfirmation.DTOs;

/// <summary>
/// Deliberately just the raw token — see docs/EMAIL_CONFIRMATION.md "Request
/// contract". No OrganizationId/UserId is accepted from the client; the token hash
/// alone identifies the tenant and user, resolved entirely inside the stored
/// procedure.
/// </summary>
public sealed record ConfirmEmailRequest(string Token);

/// <summary>
/// True for both a fresh confirmation and the idempotent "already confirmed" case —
/// the client cannot and should not distinguish them (see "Idempotency behavior").
/// </summary>
public sealed record ConfirmEmailResponse(bool IsConfirmed);

public sealed record ResendEmailConfirmationRequest(string Email);

/// <summary>
/// Deliberately empty — the generic response text lives in the outer ApiResponse
/// envelope's Message, not duplicated here. See "Resend response" — this endpoint
/// never varies its shape based on whether an account exists.
/// </summary>
public sealed record ResendEmailConfirmationResponse;
