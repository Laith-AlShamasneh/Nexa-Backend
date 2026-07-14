namespace Application.Features.Onboarding.DTOs;

public sealed record OnboardingStepDto(
    string    StepKey,
    string    Name,
    int       SortOrder,
    bool      IsRequired,
    bool      CanSkip,
    string    PagePath,
    byte      StepStatus,
    DateTime? StepCompletedAtUtc
);

public sealed record OnboardingStateResponse(
    string                           CurrentStepKey,
    byte                             Status,
    DateTime                         StartedAtUtc,
    DateTime?                        CompletedAtUtc,
    IReadOnlyList<OnboardingStepDto> Steps
);

public sealed record AdvanceStepRequest(
    string StepKey,
    bool   IsSkip
);
