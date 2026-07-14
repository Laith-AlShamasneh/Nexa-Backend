using Application.Features.Onboarding.DTOs;
using Shared.Results;

namespace Application.Interfaces.Services;

public interface IOnboardingService
{
    Task InitializeAsync(long userId, CancellationToken ct = default);
    Task<ServiceResult<OnboardingStateResponse>> GetStateAsync(CancellationToken ct = default);
    Task<ServiceResult<bool>> AdvanceStepAsync(AdvanceStepRequest request, CancellationToken ct = default);
    Task<ServiceResult<bool>> SkipAsync(CancellationToken ct = default);
}
