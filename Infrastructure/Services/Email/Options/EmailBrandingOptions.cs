namespace Infrastructure.Services.Email.Options;

/// <summary>
/// Brand identity shared by every email template's header/footer — kept out of
/// individual template files so a rebrand (name, support address) is a one-line
/// config change, not an edit to every template. Bound from "Email:Branding".
/// </summary>
public sealed class EmailBrandingOptions
{
    public string CompanyName  { get; init; } = "Nexa";
    public string SupportEmail { get; init; } = string.Empty;
    public string WebsiteUrl   { get; init; } = string.Empty;
}
