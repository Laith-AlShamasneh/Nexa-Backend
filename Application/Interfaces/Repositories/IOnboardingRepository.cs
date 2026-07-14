using Application.Features.Onboarding.DbModels;

namespace Application.Interfaces.Repositories;

public interface IOnboardingRepository
{
    Task InitializeAsync(long userId, CancellationToken ct = default);
    Task<IReadOnlyList<OnboardingStateDbResult>> GetStateAsync(long userId, CancellationToken ct = default);
    Task<AdvanceStepDbResult> AdvanceStepAsync(AdvanceStepDbInput input, CancellationToken ct = default);
    Task<SkipOnboardingDbResult> SkipAsync(long userId, CancellationToken ct = default);
}
