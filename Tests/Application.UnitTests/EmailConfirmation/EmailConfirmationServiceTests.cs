using Application.Common.Options;
using Application.Features.Email.Jobs;
using Application.Features.EmailConfirmation.DbModels;
using Application.Features.EmailConfirmation.DTOs;
using Application.Features.EmailConfirmation.Services;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Enums.System;
using Xunit;

namespace Application.UnitTests.EmailConfirmation;

public sealed class EmailConfirmationServiceTests
{
    private static (
        EmailConfirmationService Service,
        Mock<IEmailConfirmationRepository> Repository,
        Mock<ITokenHasher> TokenHasher,
        Mock<IBackgroundJobService> Jobs) BuildService()
    {
        var repository = new Mock<IEmailConfirmationRepository>();

        var tokenHasher = new Mock<ITokenHasher>();
        tokenHasher.Setup(h => h.GenerateRawToken()).Returns("raw-token-value");
        tokenHasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string t) => $"HASHED({t})");

        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(d => d.UtcNow).Returns(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var userContext = new Mock<IUserContext>();
        userContext.SetupGet(u => u.IpAddress).Returns("127.0.0.1");
        userContext.SetupGet(u => u.TraceId).Returns((string?)null);
        userContext.SetupGet(u => u.Language).Returns(SystemLanguages.English);

        var messageProvider = new Mock<IMessageProvider>();
        messageProvider
            .Setup(m => m.GetMessagesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) => key);

        var jobs = new Mock<IBackgroundJobService>();

        var authOptions = Options.Create(new AuthenticationOptions
        {
            EmailConfirmationExpiryHours = 24,
            ConfirmEmailBaseUrl = "https://app.nexa.local/confirm-email"
        });

        var confirmationOptions = Options.Create(new EmailConfirmationOptions
        {
            ResendCooldownSeconds = 120,
            MaxResendsPerHour = 5
        });

        var service = new EmailConfirmationService(
            repository.Object,
            tokenHasher.Object,
            dateTimeProvider.Object,
            userContext.Object,
            messageProvider.Object,
            jobs.Object,
            authOptions,
            confirmationOptions,
            NullLogger<EmailConfirmationService>.Instance);

        return (service, repository, tokenHasher, jobs);
    }

    // ── Confirm ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task ConfirmAsync_HashesRawTokenBeforeLookup_NeverPassesRawTokenToRepository()
    {
        var (service, repository, _, _) = BuildService();
        repository
            .Setup(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailDbResult { ResultCode = 0, UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() });

        await service.ConfirmAsync(new ConfirmEmailRequest("raw-token-value"), CancellationToken.None);

        repository.Verify(r => r.ConfirmAsync(
            It.Is<ConfirmEmailDbInput>(i =>
                i.TokenHash == "HASHED(raw-token-value)" &&
                i.TokenHash != "raw-token-value"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConfirmAsync_ResultCodeZero_ReturnsSuccessWithIsConfirmedTrue()
    {
        var (service, repository, _, _) = BuildService();
        repository
            .Setup(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailDbResult { ResultCode = 0, UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() });

        var result = await service.ConfirmAsync(new ConfirmEmailRequest("token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsConfirmed);
    }

    [Fact]
    public async Task ConfirmAsync_ResultCodeOne_AlreadyConfirmed_IsIdempotentSuccess()
    {
        var (service, repository, _, _) = BuildService();
        repository
            .Setup(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailDbResult { ResultCode = 1, UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() });

        var result = await service.ConfirmAsync(new ConfirmEmailRequest("token"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Data!.IsConfirmed);
    }

    [Theory]
    [InlineData(2)]  // Invalid: not found / expired / revoked / user-or-org ineligible — all collapsed
    [InlineData(99)] // Any unrecognized code must fail safe, not succeed
    public async Task ConfirmAsync_ResultCodeInvalid_ReturnsGenericFailure_NoStateLeaked(int resultCode)
    {
        var (service, repository, _, _) = BuildService();
        repository
            .Setup(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmEmailDbResult { ResultCode = resultCode, UserId = null, OrganizationId = null });

        var result = await service.ConfirmAsync(new ConfirmEmailRequest("token"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Authentication.InvalidToken", result.Message);
    }

    [Fact]
    public async Task ConfirmAsync_PropagatesCancellationToken()
    {
        var (service, repository, _, _) = BuildService();
        using var cts = new CancellationTokenSource();
        repository
            .Setup(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), cts.Token))
            .ReturnsAsync(new ConfirmEmailDbResult { ResultCode = 0, UserId = Guid.NewGuid(), OrganizationId = Guid.NewGuid() });

        var result = await service.ConfirmAsync(new ConfirmEmailRequest("token"), cts.Token);

        Assert.True(result.IsSuccess);
        repository.Verify(r => r.ConfirmAsync(It.IsAny<ConfirmEmailDbInput>(), cts.Token), Times.Once);
    }

    // ── Resend ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task ResendAsync_AlwaysReturnsGenericSuccessMessage_RegardlessOfEligibility()
    {
        var (service, repository, _, _) = BuildService();

        foreach (var resultCode in new[] { 0, 1, 2, 3 })
        {
            repository
                .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ResendEmailConfirmationDbResult
                {
                    ResultCode = resultCode,
                    UserId = resultCode == 0 ? Guid.NewGuid() : null,
                    OrganizationId = resultCode == 0 ? Guid.NewGuid() : null,
                    DisplayNameEn = "Test User"
                });

            var result = await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), CancellationToken.None);

            Assert.True(result.IsSuccess);
            Assert.Equal("Authentication.ConfirmationEmailSent", result.Message);
        }
    }

    [Fact]
    public async Task ResendAsync_NotEligible_DoesNotEnqueueEmail()
    {
        var (service, repository, _, jobs) = BuildService();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendEmailConfirmationDbResult { ResultCode = 1 }); // NotEligible

        await service.ResendAsync(new ResendEmailConfirmationRequest("nobody@example.com"), CancellationToken.None);

        jobs.Verify(j => j.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<EmailConfirmationPayload>(), It.IsAny<byte>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendAsync_CooldownActive_DoesNotEnqueueEmail()
    {
        var (service, repository, _, jobs) = BuildService();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendEmailConfirmationDbResult { ResultCode = 2 }); // CooldownActive

        await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), CancellationToken.None);

        jobs.Verify(j => j.EnqueueAsync(
            It.IsAny<string>(), It.IsAny<EmailConfirmationPayload>(), It.IsAny<byte>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResendAsync_TokenCreated_PassesOnlyHashedTokenToRepository_UsesConfiguredExpiry()
    {
        var (service, repository, _, _) = BuildService();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendEmailConfirmationDbResult
            {
                ResultCode = 0,
                UserId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                DisplayNameEn = "Test User"
            });

        await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), CancellationToken.None);

        repository.Verify(r => r.ResendAsync(
            It.Is<ResendEmailConfirmationDbInput>(i =>
                i.NewTokenHash == "HASHED(raw-token-value)" &&
                i.NewTokenHash != "raw-token-value" &&
                i.NewTokenExpiresAtUtc == new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc) && // UtcNow + 24h
                i.ResendCooldownSeconds == 120 &&
                i.MaxResendsPerHour == 5),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendAsync_TokenCreated_EnqueuesConfirmationEmailWithRawTokenInLink()
    {
        var (service, repository, _, jobs) = BuildService();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendEmailConfirmationDbResult
            {
                ResultCode = 0,
                UserId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                DisplayNameEn = "Test User"
            });

        await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), CancellationToken.None);

        // The raw token (not its hash) must be what's embedded in the emailed link —
        // that's the whole point of a confirmation link. It is never itself persisted
        // (verified above: only NewTokenHash reaches the repository).
        jobs.Verify(j => j.EnqueueAsync(
            "EmailConfirmation",
            It.Is<EmailConfirmationPayload>(p => p.ConfirmationLink.Contains("raw-token-value")),
            It.IsAny<byte>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResendAsync_EmailEnqueueThrows_StillReturnsGenericSuccess()
    {
        var (service, repository, _, jobs) = BuildService();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResendEmailConfirmationDbResult
            {
                ResultCode = 0,
                UserId = Guid.NewGuid(),
                OrganizationId = Guid.NewGuid(),
                DisplayNameEn = "Test User"
            });
        jobs
            .Setup(j => j.EnqueueAsync(
                It.IsAny<string>(), It.IsAny<EmailConfirmationPayload>(), It.IsAny<byte>(), It.IsAny<DateTime?>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP pipeline unavailable"));

        var result = await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), CancellationToken.None);

        // Token was already persisted by the repository call above — a delivery
        // failure must not surface as an error response (see
        // docs/EMAIL_CONFIRMATION.md "Email delivery failure strategy").
        Assert.True(result.IsSuccess);
        Assert.Equal("Authentication.ConfirmationEmailSent", result.Message);
    }

    [Fact]
    public async Task ResendAsync_PropagatesCancellationToken()
    {
        var (service, repository, _, _) = BuildService();
        using var cts = new CancellationTokenSource();
        repository
            .Setup(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), cts.Token))
            .ReturnsAsync(new ResendEmailConfirmationDbResult { ResultCode = 1 });

        await service.ResendAsync(new ResendEmailConfirmationRequest("owner@example.com"), cts.Token);

        repository.Verify(r => r.ResendAsync(It.IsAny<ResendEmailConfirmationDbInput>(), cts.Token), Times.Once);
    }
}
