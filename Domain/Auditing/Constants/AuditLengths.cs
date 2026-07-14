namespace Domain.Auditing.Constants;

/// <summary>Column length limits matching `audit.AuditLogs` (migrations 007, 009).</summary>
public static class AuditLengths
{
    public const int ActionMaxLength = 100;
    public const int EntityNameMaxLength = 150;
    public const int EntityIdMaxLength = 100;
    public const int IpAddressMaxLength = 45;
    public const int RequestIdMaxLength = 100;
    public const int SourceMaxLength = 100;
    public const int UserAgentMaxLength = 500;
    public const int FailureReasonMaxLength = 500;
}
