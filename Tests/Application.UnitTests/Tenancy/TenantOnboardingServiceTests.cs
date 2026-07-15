using Application.Common.Options;
using Application.Features.Email.Jobs;
using Application.Features.Tenancy.DbModels;
using Application.Features.Tenancy.DTOs;
using Application.Features.Tenancy.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Enums.System;
using Xunit;

namespace Application.UnitTests.Tenancy;

public sealed class TenantOnboardingServiceTests
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

    private static (
        TenantOnboardingService Service,
        Mock<IOrganizationRegistrationRepository> Repository,
        Mock<IBackgroundJobService> Jobs) BuildService(RegisterOrganizationDbResult dbResult)
    {
        var repository = new Mock<IOrganizationRegistrationRepository>();
        repository
            .Setup(r => r.RegisterAsync(It.IsAny<RegisterOrganizationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbResult);

        var passwordHasher = new Mock<IPasswordHasher>();
        passwordHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"HASHED({p})");

        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(h => h.GenerateRawToken()).Returns("raw-token-value");
        tokenHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string t) => $"HASHED({t})");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var userContext = new Mock<IUserContext>();
        userContext.SetupGet(u => u.IpAddress).Returns("127.0.0.1");
        userContext.SetupGet(u => u.TraceId).Returns((string?)null);

        var messageProvider = new Mock<IMessageProvider>();
        messageProvider
            .Setup(m => m.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => key);

        var jobs = new Mock<IBackgroundJobService>();

        var fileService = new Mock<IFileService>();
        var storageUtility = new Mock<IStorageUtility>();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            EmailConfirmationExpiryHours = 24,
            ConfirmEmailBaseUrl = "https://app.nexa.local/confirm-email"
        });

        var service = new TenantOnboardingService(
            repository.Object,
            passwordHasher.Object,
            tokenHasher.Object,
            dateTimeProvider.Object,
            fileService.Object,
            storageUtility.Object,
            userContext.Object,
            messageProvider.Object,
            jobs.Object,
            authOptions,
            NullLogger<TenantOnboardingService>.Instance);

        return (service, repository, jobs);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_CallsRepositoryWithHashedPasswordAndTokenHashOnly()
    {
        var dbResult = new RegisterOrganizationDbResult
        {
            ResultCode = 0,
            OrganizationId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            OwnerPersonId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            OwnerRoleId = Guid.NewGuid(),
            EmailConfirmationTokenId = 1,
            CreatedAt = DateTime.UtcNow
        };
        var (service, repository, _) = BuildService(dbResult);
        var request = ValidRequest();

        var result = await service.RegisterAsync(request, CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.Verify(r => r.RegisterAsync(
            It.Is<RegisterOrganizationDbInput>(i =>
                i.PasswordHash == "HASHED(P@ssw0rd1)" &&                 // password is hashed, never raw
                i.PasswordHash != request.Password &&
                i.EmailConfirmationTokenHash == "HASHED(raw-token-value)" && // only the token hash is persisted
                i.EmailConfirmationTokenHash != "raw-token-value"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_ValidRequest_EnqueuesConfirmationEmailWithRawTokenInLink()
    {
        var dbResult = new RegisterOrganizationDbResult
        {
            ResultCode = 0,
            OrganizationId = Guid.NewGuid(),
            BranchId = Guid.NewGuid(),
            OwnerPersonId = Guid.NewGuid(),
            OwnerUserId = Guid.NewGuid(),
            OwnerRoleId = Guid.NewGuid(),
            EmailConfirmationTokenId = 1,
            CreatedAt = DateTime.UtcNow
        };
        var (service, _, jobs) = BuildService(dbResult);

        await service.RegisterAsync(ValidRequest(), CancellationToken.None);

        jobs.Verify(j => j.EnqueueAsync(
            "EmailConfirmation",
            It.Is<EmailConfirmationPayload>(p => p.ConfirmationLink.Contains("raw-token-value")),
            It.IsAny<byte>(),
            It.IsAny<DateTime?>(),
            It.IsAny<int>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_MissingRoleTemplates_ReturnsInternalServerErrorFailure()
    {
        var (service, _, _) = BuildService(new RegisterOrganizationDbResult { ResultCode = 2 });

        var result = await service.RegisterAsync(ValidRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(InternalResponseCodes.InternalServerError, result.Code);
    }

    [Fact]
    public async Task RegisterAsync_SlugConflict_ReturnsConflictFailure()
    {
        var (service, _, _) = BuildService(new RegisterOrganizationDbResult { ResultCode = 1 });

        var result = await service.RegisterAsync(ValidRequest(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(InternalResponseCodes.Conflict, result.Code);
    }
}
