using Domain.Common;
using Domain.Exceptions;
using Domain.Identity.Constants;

namespace Domain.Identity.Entities;

/// <summary>
/// An append-only sign-in attempt record — no update or delete behavior exists here
/// on purpose (see docs/ARCHITECTURE_RULES.md, "append-only security/audit records
/// must remain append-only"). <see cref="OrganizationId"/>/<see cref="UserId"/> are
/// nullable because a failed login against an unknown account, or an org that
/// couldn't be resolved yet, must still be logged — this entity does not implement
/// <see cref="ITenantOwned"/> for that reason. The database intentionally carries no
/// foreign keys from this table (see docs/database/DATABASE_FINAL_BLUEPRINT.md §7):
/// it must never block on, or be blocked by, deleting the account it references.
/// </summary>
public sealed class SignInLog : Entity<long>
{
    public Guid? OrganizationId { get; }
    public Guid? UserId { get; }
    public string EmailAttempted { get; }
    public string? NormalizedEmailAttempted { get; private set; }
    public bool IsSuccessful { get; }
    public string? FailureReason { get; }
    public string EventType { get; }
    public string? AuthenticationMethod { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public string? DeviceId { get; }
    public Guid? CorrelationId { get; }
    public DateTime CreatedAt { get; }

    private SignInLog(long id, Guid? organizationId, Guid? userId, string emailAttempted, bool isSuccessful,
        string? failureReason, string eventType, string? authenticationMethod, string? ipAddress, string? userAgent,
        string? deviceId, Guid? correlationId, DateTime createdAt) : base(id)
    {
        OrganizationId = organizationId;
        UserId = userId;
        EmailAttempted = emailAttempted;
        IsSuccessful = isSuccessful;
        FailureReason = failureReason;
        EventType = eventType;
        AuthenticationMethod = authenticationMethod;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        DeviceId = deviceId;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
    }

    public static SignInLog Successful(Guid organizationId, Guid userId, string emailAttempted,
        string eventType = SignInEventTypes.PasswordSignIn, string? authenticationMethod = null,
        string? ipAddress = null, string? userAgent = null, string? deviceId = null, Guid? correlationId = null)
    {
        return new SignInLog(0, organizationId, userId, GuardEmail(emailAttempted), true, null,
            GuardEventType(eventType), authenticationMethod, ipAddress, userAgent, deviceId, correlationId, DateTime.UtcNow);
    }

    public static SignInLog Failed(string emailAttempted, string? failureReason, Guid? organizationId = null,
        Guid? userId = null, string eventType = SignInEventTypes.PasswordSignIn, string? authenticationMethod = null,
        string? ipAddress = null, string? userAgent = null, string? deviceId = null, Guid? correlationId = null)
    {
        return new SignInLog(0, organizationId, userId, GuardEmail(emailAttempted), false,
            GuardFailureReason(failureReason), GuardEventType(eventType), authenticationMethod, ipAddress, userAgent,
            deviceId, correlationId, DateTime.UtcNow);
    }

    public static SignInLog Reconstitute(
        long id, Guid? organizationId, Guid? userId, string emailAttempted, string? normalizedEmailAttempted,
        bool isSuccessful, string? failureReason, string eventType, string? authenticationMethod, string? ipAddress,
        string? userAgent, string? deviceId, Guid? correlationId, DateTime createdAt)
    {
        return new SignInLog(id, organizationId, userId, emailAttempted, isSuccessful, failureReason, eventType,
            authenticationMethod, ipAddress, userAgent, deviceId, correlationId, createdAt)
        {
            NormalizedEmailAttempted = normalizedEmailAttempted
        };
    }

    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("SignInLog Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    private static string GuardEmail(string emailAttempted)
    {
        if (string.IsNullOrWhiteSpace(emailAttempted))
            throw new ValidationAppException("EmailAttempted cannot be empty.");
        var trimmed = emailAttempted.Trim();
        if (trimmed.Length > IdentityLengths.SignInLog.EmailAttemptedMaxLength)
            throw new ValidationAppException($"EmailAttempted cannot exceed {IdentityLengths.SignInLog.EmailAttemptedMaxLength} characters.");
        return trimmed;
    }

    private static string GuardEventType(string eventType)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ValidationAppException("EventType cannot be empty.");
        if (eventType.Length > IdentityLengths.SignInLog.EventTypeMaxLength)
            throw new ValidationAppException($"EventType cannot exceed {IdentityLengths.SignInLog.EventTypeMaxLength} characters.");
        return eventType;
    }

    private static string? GuardFailureReason(string? failureReason)
    {
        if (failureReason is { Length: > 0 } && failureReason.Length > IdentityLengths.SignInLog.FailureReasonMaxLength)
            throw new ValidationAppException($"FailureReason cannot exceed {IdentityLengths.SignInLog.FailureReasonMaxLength} characters.");
        return failureReason;
    }
}
