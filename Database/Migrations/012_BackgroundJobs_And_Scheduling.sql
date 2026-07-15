/*
    Migration 012: Background Jobs and Scheduled Jobs

    Two related pieces of system-level (non-tenant-owned, `dbo` schema)
    infrastructure that the C# codebase already has working, compiled
    plumbing for but no database objects behind:

    1. dbo.BackgroundJobs — a fire-and-forget work queue. Design and stored
       procedures (Enqueue/PickUp/Complete/Fail) are ported from MyMoney's
       proven implementation (dedup-aware enqueue, atomic UPDLOCK/READPAST
       claim, exponential-backoff retry), adapted to Nexa conventions:
         - GUID actor columns (CreatedBy) instead of MyMoney's BIGINT, to
           match identity.Users.Id — see docs "Known cross-layer
           inconsistency" (IUserContext.UserId is still long; reconciling
           the C# side is deferred to when this table gets wired up).
         - DATETIME2(3)/SYSUTCDATETIME() throughout, matching every other
           Nexa table, instead of MyMoney's DATETIME2(0)/GETUTCDATE().
         - An optional OrganizationId column: MyMoney was single-tenant so
           had no such concept. Most jobs will still carry their tenant
           context inside the JSON Payload (matching the existing
           BackgroundJobEnqueueInput contract, which has no OrganizationId
           parameter), so this column is nullable and purely for
           filtering/cleanup (e.g. "cancel all pending jobs for organization
           X" when an org is deleted) — not a required input.
         - CREATE OR ALTER from the start, and folded into one migration,
           instead of MyMoney's separate later migration that bolted on
           DedupKey after the fact.

       Stored procedure signatures exactly match what
       Infrastructure/Jobs/BackgroundJobRepository.cs already calls:
       dbo.usp_BackgroundJob_Enqueue/PickUp/Complete/Fail.

    2. dbo.ScheduledJobs — new. MyMoney had no recurring-job concept at the
       database level at all; every recurring behavior was a hardcoded
       ASP.NET Core hosted-service timer in C#, which has a real
       correctness gap: if the API ever scales to more than one instance,
       every instance's timer fires independently and the same recurring
       action runs multiple times (duplicate emails, duplicate reports).
       ScheduledJobs stores recurring-job *definitions* (cron expression or
       simple interval) and is claimed via the same atomic
       UPDLOCK/READPAST pattern as BackgroundJobs pickup, so only one
       instance ever claims a given due schedule. Cron/interval-to-next-run
       computation is intentionally left to the C# caller (a cron parser in
       T-SQL is fragile); the stored procedures only handle atomic claim
       and atomic completion (advance NextRunAt, record what got enqueued).

    Both tables live in `dbo` (not a tenant schema) because they are
    system/operational infrastructure, matching the existing
    dbo.SchemaVersions precedent, and because BackgroundJobRepository.cs
    already hardcodes the `dbo.` prefix on every call.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'012')
BEGIN
    PRINT N'Migration 012 has already been applied; nothing to do.';
    RETURN;
END;
GO

BEGIN TRANSACTION Migration012;

---------------------------------------------------------------------------
-- dbo.BackgroundJobs
---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BackgroundJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BackgroundJobs
    (
        JobId          BIGINT           NOT NULL IDENTITY(1,1),
        OrganizationId UNIQUEIDENTIFIER NULL,
        JobType        NVARCHAR(200)    NOT NULL,
        Payload        NVARCHAR(MAX)    NOT NULL,
        StatusId       TINYINT          NOT NULL CONSTRAINT DF_BackgroundJobs_StatusId DEFAULT 1,
        Priority       TINYINT          NOT NULL CONSTRAINT DF_BackgroundJobs_Priority DEFAULT 2,
        ScheduledAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_BackgroundJobs_ScheduledAt DEFAULT SYSUTCDATETIME(),
        PickedUpAt     DATETIME2(3)     NULL,
        CompletedAt    DATETIME2(3)     NULL,
        AttemptCount   INT              NOT NULL CONSTRAINT DF_BackgroundJobs_AttemptCount DEFAULT 0,
        MaxAttempts    INT              NOT NULL CONSTRAINT DF_BackgroundJobs_MaxAttempts DEFAULT 3,
        LastAttemptAt  DATETIME2(3)     NULL,
        NextRetryAt    DATETIME2(3)     NULL,
        ErrorMessage   NVARCHAR(MAX)    NULL,
        DedupKey       NVARCHAR(200)    NULL,
        CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_BackgroundJobs_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy      UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_BackgroundJobs PRIMARY KEY CLUSTERED (JobId),
        CONSTRAINT CK_BackgroundJobs_StatusId CHECK (StatusId BETWEEN 1 AND 5),
        CONSTRAINT CK_BackgroundJobs_Priority CHECK (Priority BETWEEN 1 AND 3),
        CONSTRAINT CK_BackgroundJobs_AttemptCount CHECK (AttemptCount >= 0),
        CONSTRAINT CK_BackgroundJobs_MaxAttempts CHECK (MaxAttempts >= 1)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BackgroundJobs_Status_Scheduled' AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
    CREATE INDEX IX_BackgroundJobs_Status_Scheduled ON dbo.BackgroundJobs (StatusId, ScheduledAt)
        WHERE StatusId IN (1, 4);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BackgroundJobs_NextRetryAt' AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
    CREATE INDEX IX_BackgroundJobs_NextRetryAt ON dbo.BackgroundJobs (NextRetryAt)
        WHERE StatusId = 4 AND NextRetryAt IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_BackgroundJobs_OrganizationId' AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
    CREATE INDEX IX_BackgroundJobs_OrganizationId ON dbo.BackgroundJobs (OrganizationId)
        WHERE OrganizationId IS NOT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_BackgroundJobs_DedupKey_Active' AND object_id = OBJECT_ID(N'dbo.BackgroundJobs'))
    CREATE UNIQUE INDEX UX_BackgroundJobs_DedupKey_Active ON dbo.BackgroundJobs (DedupKey)
        WHERE DedupKey IS NOT NULL AND StatusId < 3;

---------------------------------------------------------------------------
-- dbo.ScheduledJobs — recurring-job definitions (new; MyMoney had none)
---------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.ScheduledJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ScheduledJobs
    (
        ScheduledJobId     BIGINT           NOT NULL IDENTITY(1,1),
        OrganizationId     UNIQUEIDENTIFIER NULL,
        Name               NVARCHAR(200)    NOT NULL,
        Description        NVARCHAR(500)    NULL,
        JobType            NVARCHAR(200)    NOT NULL,
        PayloadTemplate    NVARCHAR(MAX)    NULL,
        CronExpression     NVARCHAR(100)    NULL,
        IntervalSeconds    INT              NULL,
        Priority           TINYINT          NOT NULL CONSTRAINT DF_ScheduledJobs_Priority DEFAULT 2,
        MaxAttempts        INT              NOT NULL CONSTRAINT DF_ScheduledJobs_MaxAttempts DEFAULT 3,
        IsEnabled          BIT              NOT NULL CONSTRAINT DF_ScheduledJobs_IsEnabled DEFAULT 1,
        NextRunAt          DATETIME2(3)     NOT NULL,
        LastRunAt          DATETIME2(3)     NULL,
        LastEnqueuedJobId  BIGINT           NULL,
        IsClaimed          BIT              NOT NULL CONSTRAINT DF_ScheduledJobs_IsClaimed DEFAULT 0,
        ClaimedAt          DATETIME2(3)     NULL,
        CreatedAt          DATETIME2(3)     NOT NULL CONSTRAINT DF_ScheduledJobs_CreatedAt DEFAULT SYSUTCDATETIME(),
        CreatedBy          UNIQUEIDENTIFIER NULL,
        UpdatedAt          DATETIME2(3)     NULL,
        UpdatedBy          UNIQUEIDENTIFIER NULL,
        CONSTRAINT PK_ScheduledJobs PRIMARY KEY CLUSTERED (ScheduledJobId),
        CONSTRAINT FK_ScheduledJobs_LastEnqueuedJob FOREIGN KEY (LastEnqueuedJobId)
            REFERENCES dbo.BackgroundJobs (JobId),
        CONSTRAINT CK_ScheduledJobs_Priority CHECK (Priority BETWEEN 1 AND 3),
        CONSTRAINT CK_ScheduledJobs_MaxAttempts CHECK (MaxAttempts >= 1),
        CONSTRAINT CK_ScheduledJobs_IntervalSeconds CHECK (IntervalSeconds IS NULL OR IntervalSeconds > 0),
        CONSTRAINT CK_ScheduledJobs_ScheduleSource CHECK (
            (CASE WHEN CronExpression IS NOT NULL THEN 1 ELSE 0 END)
            + (CASE WHEN IntervalSeconds IS NOT NULL THEN 1 ELSE 0 END) = 1
        )
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ScheduledJobs_Due' AND object_id = OBJECT_ID(N'dbo.ScheduledJobs'))
    CREATE INDEX IX_ScheduledJobs_Due ON dbo.ScheduledJobs (NextRunAt)
        WHERE IsEnabled = 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_ScheduledJobs_Name_Org' AND object_id = OBJECT_ID(N'dbo.ScheduledJobs'))
    CREATE UNIQUE INDEX UX_ScheduledJobs_Name_Org ON dbo.ScheduledJobs (Name, OrganizationId);

---------------------------------------------------------------------------
-- Migration metadata (register before COMMIT — same pattern as 010)
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'012')
BEGIN
    INSERT INTO dbo.SchemaVersions (MigrationId, [Name], [Checksum])
    VALUES (N'012', N'Background Jobs and Scheduled Jobs', NULL);
END;

COMMIT TRANSACTION Migration012;
GO

---------------------------------------------------------------------------
-- Stored procedures (own batches — CREATE/ALTER PROCEDURE must be the
-- first statement in a batch, so these sit outside the DDL transaction
-- above; each is independently idempotent via CREATE OR ALTER).
---------------------------------------------------------------------------

CREATE OR ALTER PROCEDURE dbo.usp_BackgroundJob_Enqueue
(
    @JobType        NVARCHAR(200),
    @Payload        NVARCHAR(MAX),
    @Priority       TINYINT           = 2,
    @ScheduledAt    DATETIME2(3)      = NULL,
    @MaxAttempts    INT               = 3,
    @CreatedBy      UNIQUEIDENTIFIER  = NULL,
    @DedupKey       NVARCHAR(200)     = NULL,
    @OrganizationId UNIQUEIDENTIFIER  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    -- Deliberately no SET XACT_ABORT ON here: the CATCH block below treats a
    -- unique-constraint violation (2601/2627 on UX_BackgroundJobs_DedupKey_Active)
    -- as a normal dedup outcome, not a fatal error. XACT_ABORT ON would doom the
    -- caller's ambient transaction the instant that violation occurs, making the
    -- CATCH block's recovery unusable to callers running inside their own
    -- transaction (e.g. INSERT ... EXEC).

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

    IF @ScheduledAt IS NULL
        SET @ScheduledAt = @Now;

    BEGIN TRY
        INSERT INTO dbo.BackgroundJobs
            (OrganizationId, JobType, Payload, StatusId, Priority, ScheduledAt, MaxAttempts, DedupKey, CreatedAt, CreatedBy)
        VALUES
            (@OrganizationId, @JobType, @Payload, 1, @Priority, @ScheduledAt, @MaxAttempts, @DedupKey, @Now, @CreatedBy);

        SELECT 0 AS ResultCode, SCOPE_IDENTITY() AS JobId;
    END TRY
    BEGIN CATCH
        -- 2601/2627: unique-index violation on UX_BackgroundJobs_DedupKey_Active.
        -- An identical, still-pending/still-failing job already exists — treat
        -- this as a successful dedup, not an error (matches MyMoney's behavior).
        IF ERROR_NUMBER() IN (2601, 2627) AND @DedupKey IS NOT NULL
        BEGIN
            SELECT
                1 AS ResultCode,
                (SELECT TOP (1) JobId FROM dbo.BackgroundJobs
                 WHERE DedupKey = @DedupKey AND StatusId < 3
                 ORDER BY JobId DESC) AS JobId;
        END
        ELSE
        BEGIN
            THROW;
        END;
    END CATCH;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BackgroundJob_PickUp
(
    @BatchSize INT = 20
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Claimed TABLE (JobId BIGINT PRIMARY KEY);

    -- UPDATE has no ORDER BY of its own, so priority ordering happens in this
    -- CTE (TOP + ORDER BY) and the UPDATE targets the CTE — UPDLOCK/READPAST
    -- on the base table still applies, keeping the claim atomic across
    -- concurrent pickers.
    ;WITH Claimable AS
    (
        SELECT TOP (@BatchSize) *
        FROM dbo.BackgroundJobs WITH (UPDLOCK, READPAST)
        WHERE (StatusId = 1 AND ScheduledAt <= @Now)
           OR (StatusId = 4 AND AttemptCount < MaxAttempts AND NextRetryAt <= @Now)
        ORDER BY Priority ASC, ScheduledAt ASC
    )
    UPDATE Claimable
        SET StatusId = 2,
            PickedUpAt = @Now,
            AttemptCount = AttemptCount + 1,
            LastAttemptAt = @Now
    OUTPUT INSERTED.JobId INTO @Claimed;

    SELECT j.JobId, j.JobType, j.Payload, j.AttemptCount, j.MaxAttempts
    FROM dbo.BackgroundJobs AS j
    INNER JOIN @Claimed AS c ON c.JobId = j.JobId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BackgroundJob_Complete
(
    @JobId BIGINT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.BackgroundJobs
        SET StatusId = 3,
            CompletedAt = SYSUTCDATETIME(),
            ErrorMessage = NULL,
            NextRetryAt = NULL
    WHERE JobId = @JobId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_BackgroundJob_Fail
(
    @JobId        BIGINT,
    @ErrorMessage NVARCHAR(MAX),
    @AttemptCount INT,
    @MaxAttempts  INT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.BackgroundJobs
        SET StatusId = 4,
            ErrorMessage = @ErrorMessage,
            NextRetryAt = CASE
                WHEN @AttemptCount < @MaxAttempts
                    THEN DATEADD(MINUTE, POWER(2, @AttemptCount), SYSUTCDATETIME())
                ELSE NULL
            END
    WHERE JobId = @JobId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ScheduledJob_Create
(
    @Name            NVARCHAR(200),
    @Description     NVARCHAR(500)     = NULL,
    @JobType         NVARCHAR(200),
    @PayloadTemplate NVARCHAR(MAX)     = NULL,
    @CronExpression  NVARCHAR(100)     = NULL,
    @IntervalSeconds INT               = NULL,
    @Priority        TINYINT           = 2,
    @MaxAttempts     INT               = 3,
    @NextRunAt       DATETIME2(3),
    @OrganizationId  UNIQUEIDENTIFIER  = NULL,
    @CreatedBy       UNIQUEIDENTIFIER  = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    INSERT INTO dbo.ScheduledJobs
        (Name, Description, JobType, PayloadTemplate, CronExpression, IntervalSeconds,
         Priority, MaxAttempts, NextRunAt, OrganizationId, CreatedAt, CreatedBy)
    VALUES
        (@Name, @Description, @JobType, @PayloadTemplate, @CronExpression, @IntervalSeconds,
         @Priority, @MaxAttempts, @NextRunAt, @OrganizationId, SYSUTCDATETIME(), @CreatedBy);

    SELECT SCOPE_IDENTITY() AS ScheduledJobId;
END;
GO

CREATE OR ALTER PROCEDURE dbo.usp_ScheduledJob_SetEnabled
(
    @ScheduledJobId BIGINT,
    @IsEnabled      BIT,
    @UpdatedBy      UNIQUEIDENTIFIER = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    UPDATE dbo.ScheduledJobs
        SET IsEnabled = @IsEnabled,
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @UpdatedBy
    WHERE ScheduledJobId = @ScheduledJobId;
END;
GO

-- Atomically claims due, enabled schedules for this instance to run.
-- A claim older than @ClaimTimeoutSeconds is treated as abandoned (the
-- owning instance crashed mid-run) and is reclaimable, mirroring
-- BackgroundJobs' retry-after-timeout safety net.
CREATE OR ALTER PROCEDURE dbo.usp_ScheduledJob_ClaimDue
(
    @BatchSize          INT = 20,
    @ClaimTimeoutSeconds INT = 300
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();
    DECLARE @Claimed TABLE (ScheduledJobId BIGINT PRIMARY KEY);

    ;WITH Claimable AS
    (
        SELECT TOP (@BatchSize) *
        FROM dbo.ScheduledJobs WITH (UPDLOCK, READPAST)
        WHERE IsEnabled = 1
          AND NextRunAt <= @Now
          AND (IsClaimed = 0 OR ClaimedAt <= DATEADD(SECOND, -@ClaimTimeoutSeconds, @Now))
        ORDER BY NextRunAt ASC
    )
    UPDATE Claimable
        SET IsClaimed = 1,
            ClaimedAt = @Now
    OUTPUT INSERTED.ScheduledJobId INTO @Claimed;

    SELECT
        s.ScheduledJobId, s.OrganizationId, s.JobType, s.PayloadTemplate,
        s.CronExpression, s.IntervalSeconds, s.Priority, s.MaxAttempts, s.NextRunAt
    FROM dbo.ScheduledJobs AS s
    INNER JOIN @Claimed AS c ON c.ScheduledJobId = s.ScheduledJobId;
END;
GO

-- Releases a claim after the caller has enqueued the corresponding
-- BackgroundJobs row and computed the schedule's next occurrence
-- (cron parsing / interval math happens in C#, not T-SQL).
CREATE OR ALTER PROCEDURE dbo.usp_ScheduledJob_CompleteRun
(
    @ScheduledJobId    BIGINT,
    @NextRunAt         DATETIME2(3),
    @LastEnqueuedJobId BIGINT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

    UPDATE dbo.ScheduledJobs
        SET IsClaimed = 0,
            ClaimedAt = NULL,
            LastRunAt = @Now,
            NextRunAt = @NextRunAt,
            LastEnqueuedJobId = COALESCE(@LastEnqueuedJobId, LastEnqueuedJobId)
    WHERE ScheduledJobId = @ScheduledJobId;
END;
GO
