namespace Domain.Tenancy.Constants;

/// <summary>Column length limits matching the `tenant` schema (migrations 002, 009).</summary>
public static class TenancyLengths
{
    public static class Organization
    {
        public const int NameMaxLength = 200;
        public const int LegalNameMaxLength = 200;
        public const int SlugMaxLength = 100;
        public const int LogoUrlMaxLength = 500;
        public const int EmailMaxLength = 256;
        public const int PhoneMaxLength = 30;
        public const int AddressMaxLength = 500;
        public const int SubscriptionPlanCodeMaxLength = 50;
    }

    public static class Branch
    {
        public const int NameMaxLength = 200;
        public const int CodeMaxLength = 50;
        public const int AddressMaxLength = 500;
        public const int PhoneMaxLength = 30;
        public const int EmailMaxLength = 256;
    }

    public static class OrganizationSettings
    {
        public const int TimeZoneIdMaxLength = 100;
        public const int DefaultLanguageCodeMaxLength = 10;
        public const int CurrencyCodeLength = 3; // CHAR(3)
        public const int DateFormatMaxLength = 30;
        public const int ReceiptPrefixMaxLength = 20;
    }
}
