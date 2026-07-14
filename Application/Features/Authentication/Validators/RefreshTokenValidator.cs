using Application.Features.Authentication.DTOs;
using FluentValidation;
using Shared.Constants;

namespace Application.Features.Authentication.Validators;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        /* Token may come from the X-Refresh-Token header instead of the body.
           Presence validation is handled in the endpoint handler after merging both sources. */
    }
}
