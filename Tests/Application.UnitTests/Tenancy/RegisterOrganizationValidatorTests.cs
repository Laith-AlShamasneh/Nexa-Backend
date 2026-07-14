using Application.Features.Tenancy.DTOs;
using Application.Features.Tenancy.Validators;
using Xunit;

namespace Application.UnitTests.Tenancy;

public sealed class RegisterOrganizationValidatorTests
{
    private static RegisterOrganizationRequest ValidRequest() => new()
    {
        OrganizationName    = "Amman English Institute",
        TimeZoneId          = "Jordan Standard Time",
        DefaultLanguageCode = "ar-JO",
        CurrencyCode        = "JOD",
        BranchName          = "Main Branch",
        FirstName           = "Laith",
        LastName            = "Owner",
        Username            = "laith.owner",
        Email               = "owner@example.com",
        Password            = "P@ssw0rd1",
        ConfirmPassword     = "P@ssw0rd1"
    };

    private readonly RegisterOrganizationValidator _validator = new();

    [Fact]
    public void Validate_ValidRequest_Passes()
    {
        var result = _validator.Validate(ValidRequest());
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_PasswordConfirmationMismatch_Fails()
    {
        var request = ValidRequest();
        request.ConfirmPassword = "SomethingDifferent1!";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterOrganizationRequest.ConfirmPassword));
    }

    [Fact]
    public void Validate_BlankOrganizationName_Fails()
    {
        var request = ValidRequest();
        request.OrganizationName = "   ";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterOrganizationRequest.OrganizationName));
    }

    [Fact]
    public void Validate_UnsupportedCurrencyCode_Fails()
    {
        var request = ValidRequest();
        request.CurrencyCode = "jod"; // must be uppercase ISO 4217

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterOrganizationRequest.CurrencyCode));
    }

    [Fact]
    public void Validate_UnknownTimeZone_Fails()
    {
        var request = ValidRequest();
        request.TimeZoneId = "Not/A/Real/Zone";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterOrganizationRequest.TimeZoneId));
    }

    [Fact]
    public void Validate_WeakPassword_Fails()
    {
        var request = ValidRequest();
        request.Password = "weak";
        request.ConfirmPassword = "weak";

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(RegisterOrganizationRequest.Password));
    }
}
