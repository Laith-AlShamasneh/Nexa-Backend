using System.Text.Json;
using Domain.Common;
using Domain.Exceptions;
using Domain.Tenancy.Constants;

namespace Domain.Tenancy.Entities;

/// <summary>
/// Per-tenant configuration (migration 009). A true 1:1 extension of
/// <see cref="Organization"/> — <see cref="OrganizationId"/> is both this entity's
/// identity and its foreign key, so it is modeled as its own small aggregate root
/// rather than a value object embedded in Organization (Dapper loads it independently
/// via a separate query). No soft delete: a settings row lives and dies with its
/// organization.
/// </summary>
public sealed class OrganizationSettings : Entity<Guid>, IAuditable
{
    public Guid OrganizationId => Id;

    public string TimeZoneId { get; private set; }
    public string DefaultLanguageCode { get; private set; }
    public string CurrencyCode { get; private set; }
    public string DateFormat { get; private set; }
    public string? ReceiptPrefix { get; private set; }
    public string? AdditionalSettingsJson { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public Guid? UpdatedBy { get; private set; }

    public byte[]? RowVersion { get; private set; }

    private const string DefaultTimeZoneId = "Asia/Amman";
    private const string DefaultLanguage = "ar-JO";
    private const string DefaultCurrency = "JOD";
    private const string DefaultDateFormatValue = "dd/MM/yyyy";

    private OrganizationSettings(Guid organizationId, string timeZoneId, string defaultLanguageCode,
        string currencyCode, string dateFormat, DateTime createdAt, Guid? createdBy) : base(organizationId)
    {
        TimeZoneId = timeZoneId;
        DefaultLanguageCode = defaultLanguageCode;
        CurrencyCode = currencyCode;
        DateFormat = dateFormat;
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }

    /// <summary>Creates the default settings row for a newly created organization.</summary>
    public static OrganizationSettings CreateDefault(Guid organizationId, Guid? createdBy = null)
    {
        if (organizationId == Guid.Empty)
            throw new ValidationAppException("OrganizationId cannot be empty.");

        return new OrganizationSettings(organizationId, DefaultTimeZoneId, DefaultLanguage, DefaultCurrency,
            DefaultDateFormatValue, DateTime.UtcNow, createdBy);
    }

    public static OrganizationSettings Reconstitute(
        Guid organizationId, string timeZoneId, string defaultLanguageCode, string currencyCode, string dateFormat,
        string? receiptPrefix, string? additionalSettingsJson, DateTime createdAt, Guid? createdBy,
        DateTime? updatedAt, Guid? updatedBy, byte[]? rowVersion)
    {
        return new OrganizationSettings(organizationId, timeZoneId, defaultLanguageCode, currencyCode, dateFormat,
            createdAt, createdBy)
        {
            ReceiptPrefix = receiptPrefix,
            AdditionalSettingsJson = additionalSettingsJson,
            UpdatedAt = updatedAt,
            UpdatedBy = updatedBy,
            RowVersion = rowVersion
        };
    }

    public void UpdateLocale(string timeZoneId, string defaultLanguageCode, string currencyCode, string dateFormat,
        Guid? updatedBy)
    {
        TimeZoneId = GuardNotBlank(timeZoneId, TenancyLengths.OrganizationSettings.TimeZoneIdMaxLength, nameof(timeZoneId));
        DefaultLanguageCode = GuardNotBlank(defaultLanguageCode, TenancyLengths.OrganizationSettings.DefaultLanguageCodeMaxLength, nameof(defaultLanguageCode));
        CurrencyCode = GuardCurrencyCode(currencyCode);
        DateFormat = GuardNotBlank(dateFormat, TenancyLengths.OrganizationSettings.DateFormatMaxLength, nameof(dateFormat));
        Touch(updatedBy);
    }

    public void UpdateReceiptPrefix(string? receiptPrefix, Guid? updatedBy)
    {
        if (receiptPrefix is { Length: > 0 } && receiptPrefix.Length > TenancyLengths.OrganizationSettings.ReceiptPrefixMaxLength)
            throw new ValidationAppException($"Receipt prefix cannot exceed {TenancyLengths.OrganizationSettings.ReceiptPrefixMaxLength} characters.");

        ReceiptPrefix = receiptPrefix;
        Touch(updatedBy);
    }

    public void UpdateAdditionalSettings(string? additionalSettingsJson, Guid? updatedBy)
    {
        if (additionalSettingsJson is not null)
        {
            try
            {
                using var _ = JsonDocument.Parse(additionalSettingsJson);
            }
            catch (JsonException)
            {
                throw new ValidationAppException("Additional settings must be valid JSON.");
            }
        }

        AdditionalSettingsJson = additionalSettingsJson;
        Touch(updatedBy);
    }

    private void Touch(Guid? updatedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        UpdatedBy = updatedBy;
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

    private static string GuardCurrencyCode(string currencyCode)
    {
        var trimmed = GuardNotBlank(currencyCode, TenancyLengths.OrganizationSettings.CurrencyCodeLength, nameof(currencyCode));
        if (trimmed.Length != TenancyLengths.OrganizationSettings.CurrencyCodeLength)
            throw new ValidationAppException($"Currency code must be exactly {TenancyLengths.OrganizationSettings.CurrencyCodeLength} letters (ISO 4217), e.g. \"JOD\".");
        return trimmed.ToUpperInvariant();
    }
}
