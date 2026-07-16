/*
    Migration 013: Email Confirmation

    No schema changes — identity.EmailConfirmationTokens and identity.Users already
    carry every column this module needs (TokenHash, ExpiresAt, UsedAt, RevokedAt,
    RevocationReason, OrganizationId; Users.IsEmailConfirmed/IsActive/IsDeleted),
    confirmed by inspecting migrations 003/005/009 before writing this file. This
    migration only adds:
      1. One supporting index for the resend cooldown/rate-limit check (a
         "most recent tokens for this user, newest first" access pattern the
         existing OrganizationId+UserId+ExpiresAt index doesn't serve well).
      2. Two stored procedures: identity.usp_EmailConfirmation_Confirm and
         identity.usp_EmailConfirmation_Resend.

    Full design rationale: docs/EMAIL_CONFIRMATION.md.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'013')
BEGIN
    PRINT N'Migration 013 has already been applied; nothing to do.';
    RETURN;
END;
GO

BEGIN TRANSACTION;

---------------------------------------------------------------------------
-- Supporting index: resend cooldown / max-attempts-per-hour checks both
-- filter by UserId and scan CreatedAt descending — the existing
-- IX_EmailConfirmationTokens_OrganizationId_UserId_ExpiresAt index leads
-- with OrganizationId and sorts by ExpiresAt, neither of which serves this
-- access pattern.
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_EmailConfirmationTokens_UserId_CreatedAt' AND object_id = OBJECT_ID(N'identity.EmailConfirmationTokens'))
    CREATE INDEX IX_EmailConfirmationTokens_UserId_CreatedAt
        ON [identity].EmailConfirmationTokens (UserId, CreatedAt DESC);

---------------------------------------------------------------------------
-- Migration metadata
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'013')
BEGIN
    INSERT INTO dbo.SchemaVersions (MigrationId, [Name], [Checksum])
    VALUES (N'013', N'Email Confirmation', NULL);
END;

COMMIT TRANSACTION;
GO

---------------------------------------------------------------------------
-- identity.usp_EmailConfirmation_Confirm
--
-- ResultCode: 0 = Confirmed (just now), 1 = AlreadyConfirmed (idempotent —
-- either this exact token was already used, or the user was confirmed by a
-- concurrent request), 2 = Invalid (not found / expired / revoked / user or
-- organization not eligible). The client only ever sees "success" (0 or 1,
-- identical response) or "generic failure" (2) — never told which specific
-- sub-case applied, to avoid leaking token/account state. FailureReason is
-- written to audit.AuditLogs for operators, never returned to the caller.
--
-- Concurrency: the token row is locked (UPDLOCK, HOLDLOCK) before any state
-- is read, so a second concurrent confirm attempt for the SAME token blocks
-- until the first transaction commits, then observes UsedAt already set and
-- takes the AlreadyConfirmed branch — the same token can never confirm the
-- email twice, even under a genuine race.
---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [identity].usp_EmailConfirmation_Confirm
(
    @TokenHash     CHAR(64),
    @UsedByIp      NVARCHAR(45)     = NULL,
    @CorrelationId UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @TokenId BIGINT, @UserId UNIQUEIDENTIFIER, @OrganizationId UNIQUEIDENTIFIER;
    DECLARE @ExpiresAt DATETIME2(3), @UsedAt DATETIME2(3), @RevokedAt DATETIME2(3);
    DECLARE @ResultCode INT;
    DECLARE @FailureReason NVARCHAR(100);

    BEGIN TRY
        BEGIN TRANSACTION;

        SELECT
            @TokenId = Id, @UserId = UserId, @OrganizationId = OrganizationId,
            @ExpiresAt = ExpiresAt, @UsedAt = UsedAt, @RevokedAt = RevokedAt
        FROM [identity].EmailConfirmationTokens WITH (UPDLOCK, HOLDLOCK)
        WHERE TokenHash = @TokenHash;

        IF @TokenId IS NULL
        BEGIN
            SET @ResultCode = 2;
            SET @FailureReason = N'TokenNotFound';
        END
        ELSE IF @UsedAt IS NOT NULL
        BEGIN
            -- Re-click of an already-used link. Confirm the user really is
            -- confirmed before calling this a friendly idempotent success —
            -- don't blindly trust the token row alone.
            IF EXISTS (SELECT 1 FROM [identity].Users WHERE Id = @UserId AND OrganizationId = @OrganizationId AND IsEmailConfirmed = 1 AND IsDeleted = 0)
            BEGIN
                SET @ResultCode = 1;
                SET @FailureReason = N'TokenAlreadyUsed';
            END
            ELSE
            BEGIN
                SET @ResultCode = 2;
                SET @FailureReason = N'TokenUsedButUserNotConfirmed';
            END
        END
        ELSE IF @RevokedAt IS NOT NULL
        BEGIN
            SET @ResultCode = 2;
            SET @FailureReason = N'TokenRevoked';
        END
        ELSE IF @ExpiresAt <= @Now
        BEGIN
            SET @ResultCode = 2;
            SET @FailureReason = N'TokenExpired';
        END
        ELSE IF NOT EXISTS (
            SELECT 1
            FROM [identity].Users AS u
            INNER JOIN tenant.Organizations AS o ON o.Id = u.OrganizationId
            WHERE u.Id = @UserId AND u.OrganizationId = @OrganizationId
              AND u.IsDeleted = 0 AND u.IsActive = 1
              AND o.IsDeleted = 0
        )
        BEGIN
            -- Covers: user soft-deleted/inactive, organization soft-deleted, or a
            -- (should-be-impossible, tenant-safe-FK-enforced) cross-tenant mismatch
            -- between the token's OrganizationId and the user's actual OrganizationId.
            SET @ResultCode = 2;
            SET @FailureReason = N'UserOrOrganizationNotEligible';
        END
        ELSE IF EXISTS (SELECT 1 FROM [identity].Users WHERE Id = @UserId AND OrganizationId = @OrganizationId AND IsEmailConfirmed = 1)
        BEGIN
            -- The user was confirmed by a different token/request between our lock
            -- and now (e.g. two valid tokens existed briefly). Revoke this now-moot
            -- token rather than leaving it active, and report the same friendly
            -- idempotent success.
            UPDATE [identity].EmailConfirmationTokens
                SET RevokedAt = @Now, RevocationReason = N'Superseded by concurrent confirmation'
            WHERE Id = @TokenId;

            SET @ResultCode = 1;
            SET @FailureReason = N'UserAlreadyConfirmedConcurrently';
        END
        ELSE
        BEGIN
            UPDATE [identity].Users
                SET IsEmailConfirmed = 1, UpdatedAt = @Now
            WHERE Id = @UserId AND OrganizationId = @OrganizationId;

            UPDATE [identity].EmailConfirmationTokens
                SET UsedAt = @Now
            WHERE Id = @TokenId;

            -- Revoke every OTHER still-active token for this user — a confirmed
            -- email must leave no other valid confirmation token behind.
            UPDATE [identity].EmailConfirmationTokens
                SET RevokedAt = @Now, RevocationReason = N'Superseded by confirmation'
            WHERE UserId = @UserId AND OrganizationId = @OrganizationId
              AND Id <> @TokenId AND UsedAt IS NULL AND RevokedAt IS NULL;

            SET @ResultCode = 0;
            SET @FailureReason = NULL;
        END

        INSERT INTO audit.AuditLogs
            (OrganizationId, UserId, Action, EntityName, EntityId, IpAddress, CorrelationId, [Source], Succeeded, FailureReason, CreatedAt)
        VALUES
            (@OrganizationId, @UserId, N'EmailConfirmation.Confirm',
             N'identity.Users', ISNULL(CONVERT(NVARCHAR(100), @UserId), N'unknown'),
             @UsedByIp, @CorrelationId, N'EmailConfirmation',
             CASE WHEN @ResultCode IN (0, 1) THEN 1 ELSE 0 END,
             @FailureReason, @Now);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT @ResultCode AS ResultCode, @UserId AS UserId, @OrganizationId AS OrganizationId;
END;
GO

---------------------------------------------------------------------------
-- identity.usp_EmailConfirmation_Resend
--
-- The application generates the raw token and passes only its hash + expiry
-- in (@NewTokenHash/@NewTokenExpiresAtUtc) — this procedure never sees a raw
-- token. It decides whether that hash is actually worth persisting (an
-- eligible user exists, not in cooldown, under the hourly cap) and reports
-- back via ResultCode; the caller only enqueues the confirmation email when
-- ResultCode = 0. Every other ResultCode maps to the exact same generic
-- success response at the API layer — this procedure's job is to decide
-- WHETHER to issue a token, never to let the caller distinguish "no such
-- account" from "already confirmed" from "cooldown" from "rate limited".
--
-- ResultCode: 0 = TokenCreated, 1 = NotEligible (no matching unconfirmed
-- active user), 2 = CooldownActive, 3 = MaxAttemptsExceeded.
--
-- Concurrency: the candidate user row is locked (UPDLOCK, HOLDLOCK) before
-- the cooldown check, so two concurrent resend requests for the same email
-- can't both pass the cooldown check and both issue a token.
---------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [identity].usp_EmailConfirmation_Resend
(
    @Email                 NVARCHAR(256),
    @NewTokenHash          CHAR(64),
    @NewTokenExpiresAtUtc  DATETIME2(3),
    @ResendCooldownSeconds INT,
    @MaxResendsPerHour     INT,
    @RequestIp             NVARCHAR(45)     = NULL,
    @CorrelationId         UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @NormalizedEmail NVARCHAR(256) = UPPER(LTRIM(RTRIM(@Email)));
    DECLARE @UserId UNIQUEIDENTIFIER, @OrganizationId UNIQUEIDENTIFIER;
    DECLARE @DisplayNameEn NVARCHAR(210), @DisplayNameAr NVARCHAR(210);
    DECLARE @ResultCode INT;
    DECLARE @FailureReason NVARCHAR(100);
    DECLARE @NewTokenId BIGINT;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- Only one eligible candidate is expected (uniqueness is per-organization,
        -- not global — see docs/EMAIL_CONFIRMATION.md "Resend lookup strategy" for
        -- why this deliberately picks the oldest match rather than rejecting a
        -- theoretical cross-tenant duplicate email outright).
        SELECT TOP (1)
            @UserId = u.Id,
            @OrganizationId = u.OrganizationId
        FROM [identity].Users AS u WITH (UPDLOCK, HOLDLOCK)
        INNER JOIN tenant.Organizations AS o ON o.Id = u.OrganizationId
        WHERE u.NormalizedEmail = @NormalizedEmail
          AND u.IsDeleted = 0
          AND u.IsActive = 1
          AND u.IsEmailConfirmed = 0
          AND o.IsDeleted = 0
        ORDER BY u.CreatedAt ASC;

        IF @UserId IS NULL
        BEGIN
            SET @ResultCode = 1;
            SET @FailureReason = N'NoEligibleAccount';
        END
        ELSE IF EXISTS (
            SELECT 1 FROM [identity].EmailConfirmationTokens WITH (UPDLOCK, HOLDLOCK)
            WHERE UserId = @UserId AND CreatedAt > DATEADD(SECOND, -@ResendCooldownSeconds, @Now)
        )
        BEGIN
            SET @ResultCode = 2;
            SET @FailureReason = N'CooldownActive';
        END
        ELSE IF (
            SELECT COUNT(*) FROM [identity].EmailConfirmationTokens
            WHERE UserId = @UserId AND CreatedAt > DATEADD(HOUR, -1, @Now)
        ) >= @MaxResendsPerHour
        BEGIN
            SET @ResultCode = 3;
            SET @FailureReason = N'MaxResendsPerHourExceeded';
        END
        ELSE
        BEGIN
            -- A confirmed email must never have more than one active token — revoke
            -- every still-active token for this user before issuing the new one.
            UPDATE [identity].EmailConfirmationTokens
                SET RevokedAt = @Now, RevocationReason = N'Superseded by resend'
            WHERE UserId = @UserId AND OrganizationId = @OrganizationId
              AND UsedAt IS NULL AND RevokedAt IS NULL;

            INSERT INTO [identity].EmailConfirmationTokens
                (UserId, OrganizationId, TokenHash, ExpiresAt, CreatedByIp, CreatedAt)
            VALUES
                (@UserId, @OrganizationId, @NewTokenHash, @NewTokenExpiresAtUtc, @RequestIp, @Now);

            SET @NewTokenId = SCOPE_IDENTITY();

            SELECT TOP (1)
                @DisplayNameEn = LTRIM(RTRIM(p.FirstName + N' ' + p.LastName)),
                @DisplayNameAr = CASE
                    WHEN p.ArabicFirstName IS NOT NULL AND p.ArabicLastName IS NOT NULL
                        THEN LTRIM(RTRIM(p.ArabicFirstName + N' ' + p.ArabicLastName))
                    ELSE NULL
                END
            FROM [identity].Persons AS p
            INNER JOIN [identity].Users AS u2
                ON u2.PersonId = p.Id AND u2.OrganizationId = p.OrganizationId
            WHERE u2.Id = @UserId AND u2.OrganizationId = @OrganizationId;

            SET @ResultCode = 0;
            SET @FailureReason = NULL;
        END

        INSERT INTO audit.AuditLogs
            (OrganizationId, UserId, Action, EntityName, EntityId, IpAddress, CorrelationId, [Source], Succeeded, FailureReason, CreatedAt)
        VALUES
            (@OrganizationId, @UserId, N'EmailConfirmation.Resend',
             N'identity.EmailConfirmationTokens', ISNULL(CONVERT(NVARCHAR(100), @NewTokenId), N'n/a'),
             @RequestIp, @CorrelationId, N'EmailConfirmation',
             CASE WHEN @ResultCode = 0 THEN 1 ELSE 0 END,
             @FailureReason, @Now);

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
        THROW;
    END CATCH;

    SELECT
        @ResultCode     AS ResultCode,
        @UserId         AS UserId,
        @OrganizationId AS OrganizationId,
        @DisplayNameEn  AS DisplayNameEn,
        @DisplayNameAr  AS DisplayNameAr;
END;
GO
