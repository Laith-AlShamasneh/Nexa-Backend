using Application.Features.EmailConfirmation.DTOs;
using Shared.Results;

namespace Application.Interfaces.Services;

public interface IEmailConfirmationService
{
    Task<ServiceResult<ConfirmEmailResponse>> ConfirmAsync(
        ConfirmEmailRequest request, CancellationToken ct = default);

    Task<ServiceResult<ResendEmailConfirmationResponse>> ResendAsync(
        ResendEmailConfirmationRequest request, CancellationToken ct = default);
}
