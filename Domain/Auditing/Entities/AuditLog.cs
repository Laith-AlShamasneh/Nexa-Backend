using System.Text.Json;
using Domain.Auditing.Constants;
using Domain.Common;
using Domain.Exceptions;

namespace Domain.Auditing.Entities;

/// <summary>
/// A single append-only audit event ("who did what to which entity"). No update or
/// delete behavior exists here on purpose — see docs/ARCHITECTURE_RULES.md. Carries
/// no foreign keys in the database (see docs/database/DATABASE_FINAL_BLUEPRINT.md §7)
/// so it can log actions against actors/entities that may since have been deleted;
/// <see cref="OrganizationId"/>/<see cref="UserId"/> are nullable because some
/// actions are platform-level, not tenant-scoped, so this entity does not implement
/// <see cref="ITenantOwned"/>.
/// </summary>
/// <remarks>
/// <b>Security rule:</b> <see cref="OldValuesJson"/>/<see cref="NewValuesJson"/> must
/// never contain password hashes, raw tokens, token hashes, secrets, or connection
/// strings. Enforcing which fields go into these snapshots is the responsibility of
/// whatever Infrastructure code builds them (it must exclude sensitive columns before
/// ever constructing an AuditLog) — this entity only validates that whatever it's
/// given is well-formed JSON, not what's in it.
/// </remarks>
public sealed class AuditLog : Entity<long>
{
    public Guid? OrganizationId { get; }
    public Guid? UserId { get; }
    public string Action { get; }
    public string EntityName { get; }
    public string EntityId { get; }
    public string? OldValuesJson { get; }
    public string? NewValuesJson { get; }
    public string? IpAddress { get; }
    public string? UserAgent { get; }
    public string? RequestId { get; }
    public Guid? CorrelationId { get; }
    public string? Source { get; }
    public bool Succeeded { get; }
    public string? FailureReason { get; }
    public DateTime CreatedAt { get; }

    private AuditLog(long id, Guid? organizationId, Guid? userId, string action, string entityName, string entityId,
        string? oldValuesJson, string? newValuesJson, string? ipAddress, string? userAgent, string? requestId,
        Guid? correlationId, string? source, bool succeeded, string? failureReason, DateTime createdAt) : base(id)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Action = action;
        EntityName = entityName;
        EntityId = entityId;
        OldValuesJson = oldValuesJson;
        NewValuesJson = newValuesJson;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        RequestId = requestId;
        CorrelationId = correlationId;
        Source = source;
        Succeeded = succeeded;
        FailureReason = failureReason;
        CreatedAt = createdAt;
    }

    public static AuditLog Record(
        string action, string entityName, string entityId, Guid? organizationId = null, Guid? userId = null,
        string? oldValuesJson = null, string? newValuesJson = null, string? ipAddress = null,
        string? userAgent = null, string? requestId = null, Guid? correlationId = null, string? source = null,
        bool succeeded = true, string? failureReason = null)
    {
        return new AuditLog(0, organizationId, userId, GuardNotBlank(action, AuditLengths.ActionMaxLength, nameof(action)),
            GuardNotBlank(entityName, AuditLengths.EntityNameMaxLength, nameof(entityName)),
            GuardNotBlank(entityId, AuditLengths.EntityIdMaxLength, nameof(entityId)),
            GuardJson(oldValuesJson), GuardJson(newValuesJson), ipAddress, userAgent, requestId, correlationId,
            source, succeeded, failureReason, DateTime.UtcNow);
    }

    public static AuditLog Reconstitute(
        long id, Guid? organizationId, Guid? userId, string action, string entityName, string entityId,
        string? oldValuesJson, string? newValuesJson, string? ipAddress, string? userAgent, string? requestId,
        Guid? correlationId, string? source, bool succeeded, string? failureReason, DateTime createdAt)
    {
        return new AuditLog(id, organizationId, userId, action, entityName, entityId, oldValuesJson, newValuesJson,
            ipAddress, userAgent, requestId, correlationId, source, succeeded, failureReason, createdAt);
    }

    public void AssignDatabaseId(long id)
    {
        if (Id != 0) throw new DomainException("AuditLog Id has already been assigned.");
        if (id <= 0) throw new ValidationAppException("Id must be positive.");
        Id = id;
    }

    private static string GuardNotBlank(string value, int maxLength, string paramName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationAppException($"{paramName} cannot be empty.");
        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new ValidationAppException($"{paramName} cannot exceed {maxLength} characters.");
        return trimmed;
    }

    private static string? GuardJson(string? json)
    {
        if (json is null) return null;
        try
        {
            using var _ = JsonDocument.Parse(json);
            return json;
        }
        catch (JsonException)
        {
            throw new ValidationAppException("Audit value snapshots must be valid JSON.");
        }
    }
}
