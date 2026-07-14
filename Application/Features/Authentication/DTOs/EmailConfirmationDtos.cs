namespace Application.Features.Authentication.DTOs;

public sealed record ConfirmEmailRequest(string Token);
public sealed record ResendConfirmationEmailRequest(string Email);
