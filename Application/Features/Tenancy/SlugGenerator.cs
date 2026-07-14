using System.Text;
using Domain.Tenancy.Constants;

namespace Application.Features.Tenancy;

/// <summary>
/// Derives a URL/subdomain-safe, globally unique <c>tenant.Organizations.Slug</c>
/// from the organization's display name. The client never supplies or sees this —
/// it's an internal tenant identifier, not something registration should ask a
/// non-technical business owner to think about. A short random suffix keeps two
/// organizations named identically from needing a retry loop; the database's
/// <c>UX_Organizations_Slug</c> unique index remains the final safety net for the
/// (astronomically unlikely) case of a collision anyway.
/// </summary>
public static class SlugGenerator
{
    public static string FromName(string organizationName)
    {
        var lowered = organizationName.Trim().ToLowerInvariant();
        var builder = new StringBuilder(lowered.Length);
        var lastWasHyphen = true; // suppress a leading hyphen

        foreach (var ch in lowered)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                lastWasHyphen = false;
            }
            else if (!lastWasHyphen)
            {
                builder.Append('-');
                lastWasHyphen = true;
            }
        }

        while (builder.Length > 0 && builder[^1] == '-')
            builder.Length--;

        var suffix = Guid.NewGuid().ToString("N")[..6];
        var maxBaseLength = TenancyLengths.Organization.SlugMaxLength - suffix.Length - 1;
        var basePart = builder.ToString();
        if (basePart.Length > maxBaseLength)
            basePart = basePart[..maxBaseLength];
        if (basePart.Length == 0)
            basePart = "org";

        return $"{basePart}-{suffix}";
    }
}
