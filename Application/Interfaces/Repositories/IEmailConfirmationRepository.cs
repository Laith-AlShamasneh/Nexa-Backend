using Application.Features.EmailConfirmation.DbModels;

namespace Application.Interfaces.Repositories;

public interface IEmailConfirmationRepository
{
    Task<ConfirmEmailDbResult> ConfirmAsync(ConfirmEmailDbInput input, CancellationToken ct = default);

    Task<ResendEmailConfirmationDbResult> ResendAsync(ResendEmailConfirmationDbInput input, CancellationToken ct = default);
}
