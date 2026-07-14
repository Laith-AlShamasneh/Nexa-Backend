namespace Domain.Identity.Constants;

/// <summary>Column length limits matching the `identity` schema (migrations 003-005, 009).</summary>
public static class IdentityLengths
{
    public static class Person
    {
        public const int FirstNameMaxLength = 100;
        public const int LastNameMaxLength = 100;
        public const int EmailMaxLength = 256;
        public const int PhoneMaxLength = 30;
        public const int ProfileImageUrlMaxLength = 500;
    }

    public static class User
    {
        public const int UsernameMaxLength = 100;
        public const int EmailMaxLength = 256;
        public const int IpAddressMaxLength = 45; // IPv6 textual max length
    }

    public static class Role
    {
        public const int NameMaxLength = 100;
        public const int DescriptionMaxLength = 500;
    }

    public static class Permission
    {
        public const int CodeMaxLength = 150;
        public const int NameMaxLength = 200;
        public const int DescriptionMaxLength = 500;
        public const int ModuleMaxLength = 100;
    }

    /// <summary>SHA-256 hex digest length — shared by every *TokenHash column.</summary>
    public const int TokenHashLength = 64;

    public static class RevocationReason
    {
        public const int MaxLength = 250;
    }

    public static class SignInLog
    {
        public const int EmailAttemptedMaxLength = 256;
        public const int FailureReasonMaxLength = 200;
        public const int UserAgentMaxLength = 500;
        public const int EventTypeMaxLength = 50;
        public const int AuthenticationMethodMaxLength = 50;
        public const int DeviceIdMaxLength = 200;
    }

    public static class UserSession
    {
        public const int DeviceIdMaxLength = 200;
        public const int DeviceNameMaxLength = 200;
        public const int UserAgentMaxLength = 500;
    }
}
