namespace Domain.Customers.Constants;

/// <summary>Column length limits matching the `crm` schema (migration 006, 009).</summary>
public static class CustomerLengths
{
    public const int CustomerCodeMaxLength = 50;
    public const int CustomerTypeMaxLength = 50;
    public const int DisplayNameMaxLength = 200;
    public const int SourceMaxLength = 100;
}
