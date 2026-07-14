namespace Application.Features.Onboarding.DbModels;

public sealed class OnboardingStateDbResult
{
    public string    CurrentStepKey     { get; init; } = null!;
    public byte      OnboardingStatus   { get; init; }
    public DateTime  StartedAtUtc       { get; init; }
    public DateTime  LastUpdatedAtUtc   { get; init; }
    public DateTime? CompletedAtUtc     { get; init; }
    public string    StepKey            { get; init; } = null!;
    public string    NameEn             { get; init; } = null!;
    public string    NameAr             { get; init; } = null!;
    public byte      SortOrder          { get; init; }
    public bool      IsRequired         { get; init; }
    public bool      CanSkip            { get; init; }
    public string    PagePath           { get; init; } = null!;
    public byte?     StepStatus         { get; init; }
    public DateTime? StepStartedAtUtc   { get; init; }
    public DateTime? StepCompletedAtUtc { get; init; }
}

public sealed class AdvanceStepDbInput
{
    public long   UserId     { get; init; }
    public string StepKey    { get; init; } = null!;
    public byte   StepStatus { get; init; }
}

public sealed class AdvanceStepDbResult
{
    public byte ResultCode { get; init; }
}

public sealed class SkipOnboardingDbResult
{
    public byte ResultCode { get; init; }
}
