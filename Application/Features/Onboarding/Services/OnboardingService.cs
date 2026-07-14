using Application.Features.Onboarding.DbModels;
using Application.Features.Onboarding.DTOs;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Shared.Enums.System;
using Shared.Results;

namespace Application.Features.Onboarding.Services;

internal sealed class OnboardingService(
    IOnboardingRepository onboardingRepository,
    IUserContext          userContext) : IOnboardingService
{
    public Task InitializeAsync(long userId, CancellationToken ct = default)
        => onboardingRepository.InitializeAsync(userId, ct);

    public async Task<ServiceResult<OnboardingStateResponse>> GetStateAsync(CancellationToken ct = default)
    {
        var rows = await onboardingRepository.GetStateAsync(userContext.UserId, ct);

        if (rows.Count == 0)
            return ServiceResultFactory.Failure<OnboardingStateResponse>(
                InternalResponseCodes.NotFound, "Onboarding state not found.");

        var first    = rows[0];
        var isArabic = userContext.Language == SystemLanguages.Arabic;

        var steps = rows
            .Select(r => new OnboardingStepDto(
                StepKey:            r.StepKey,
                Name:               isArabic ? r.NameAr : r.NameEn,
                SortOrder:          r.SortOrder,
                IsRequired:         r.IsRequired,
                CanSkip:            r.CanSkip,
                PagePath:           r.PagePath,
                StepStatus:         r.StepStatus ?? 0,
                StepCompletedAtUtc: r.StepCompletedAtUtc))
            .ToList();

        var response = new OnboardingStateResponse(
            CurrentStepKey: first.CurrentStepKey,
            Status:         first.OnboardingStatus,
            StartedAtUtc:   first.StartedAtUtc,
            CompletedAtUtc: first.CompletedAtUtc,
            Steps:          steps);

        return ServiceResultFactory.Success(response, InternalResponseCodes.OK, "Success");
    }

    public async Task<ServiceResult<bool>> AdvanceStepAsync(AdvanceStepRequest request, CancellationToken ct = default)
    {
        var stepStatus = request.IsSkip ? (byte)3 : (byte)2;  // 2 = Completed, 3 = Skipped

        var dbResult = await onboardingRepository.AdvanceStepAsync(new AdvanceStepDbInput
        {
            UserId     = userContext.UserId,
            StepKey    = request.StepKey,
            StepStatus = stepStatus
        }, ct);

        return dbResult.ResultCode == 0
            ? ServiceResultFactory.Success(true, InternalResponseCodes.OK, "Step advanced.")
            : ServiceResultFactory.Failure<bool>(InternalResponseCodes.BadRequest, "Invalid step.");
    }

    public async Task<ServiceResult<bool>> SkipAsync(CancellationToken ct = default)
    {
        var dbResult = await onboardingRepository.SkipAsync(userContext.UserId, ct);

        return dbResult.ResultCode == 0
            ? ServiceResultFactory.Success(true, InternalResponseCodes.OK, "Onboarding skipped.")
            : ServiceResultFactory.Failure<bool>(InternalResponseCodes.NotFound, "Onboarding not found.");
    }
}
