-- Migration 005: Identity schema — RefreshTokens, EmailConfirmationTokens, PasswordResetTokens, SignInLogs

CREATE TABLE [identity].RefreshTokens (
    Id                  BIGINT IDENTITY(1,1) NOT NULL,
    UserId              UNIQUEIDENTIFIER NOT NULL,
    OrganizationId      UNIQUEIDENTIFIER NOT NULL,
    TokenHash           CHAR(64)         NOT NULL,   -- SHA-256 hex digest; raw token never stored
    ExpiresAt           DATETIME2(3)     NOT NULL,
    CreatedAt           DATETIME2(3)     NOT NULL CONSTRAINT DF_RefreshTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedByIp         NVARCHAR(45)     NULL,
    RevokedAt           DATETIME2(3)     NULL,
    RevokedBy           UNIQUEIDENTIFIER NULL,
    RevokedByIp         NVARCHAR(45)     NULL,
    ReplacedByTokenHash CHAR(64)         NULL,
    CONSTRAINT PK_RefreshTokens PRIMARY KEY (Id),
    CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES [identity].Users (Id)
);
GO
CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON [identity].RefreshTokens (TokenHash);
GO
CREATE INDEX IX_RefreshTokens_UserId ON [identity].RefreshTokens (UserId);
GO
CREATE INDEX IX_RefreshTokens_ExpiresAt ON [identity].RefreshTokens (ExpiresAt);
GO

CREATE TABLE [identity].EmailConfirmationTokens (
    Id        BIGINT IDENTITY(1,1) NOT NULL,
    UserId    UNIQUEIDENTIFIER NOT NULL,
    TokenHash CHAR(64)         NOT NULL,
    ExpiresAt DATETIME2(3)     NOT NULL,
    UsedAt    DATETIME2(3)     NULL,
    CreatedAt DATETIME2(3)     NOT NULL CONSTRAINT DF_EmailConfirmationTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_EmailConfirmationTokens PRIMARY KEY (Id),
    CONSTRAINT FK_EmailConfirmationTokens_Users FOREIGN KEY (UserId) REFERENCES [identity].Users (Id)
);
GO
CREATE UNIQUE INDEX UX_EmailConfirmationTokens_TokenHash ON [identity].EmailConfirmationTokens (TokenHash);
GO
CREATE INDEX IX_EmailConfirmationTokens_UserId ON [identity].EmailConfirmationTokens (UserId);
GO

CREATE TABLE [identity].PasswordResetTokens (
    Id            BIGINT IDENTITY(1,1) NOT NULL,
    UserId        UNIQUEIDENTIFIER NOT NULL,
    TokenHash     CHAR(64)         NOT NULL,
    ExpiresAt     DATETIME2(3)     NOT NULL,
    UsedAt        DATETIME2(3)     NULL,
    RequestedByIp NVARCHAR(45)     NULL,
    CreatedAt     DATETIME2(3)     NOT NULL CONSTRAINT DF_PasswordResetTokens_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_PasswordResetTokens PRIMARY KEY (Id),
    CONSTRAINT FK_PasswordResetTokens_Users FOREIGN KEY (UserId) REFERENCES [identity].Users (Id)
);
GO
CREATE UNIQUE INDEX UX_PasswordResetTokens_TokenHash ON [identity].PasswordResetTokens (TokenHash);
GO
CREATE INDEX IX_PasswordResetTokens_UserId ON [identity].PasswordResetTokens (UserId);
GO

-- No FKs: high-volume append-only log, informational linkage only (see blueprint §7 Security Review).
CREATE TABLE [identity].SignInLogs (
    Id             BIGINT IDENTITY(1,1) NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NULL,
    UserId         UNIQUEIDENTIFIER NULL,
    EmailAttempted NVARCHAR(256)    NOT NULL,
    IsSuccessful   BIT              NOT NULL,
    FailureReason  NVARCHAR(200)    NULL,
    IpAddress      NVARCHAR(45)     NULL,
    UserAgent      NVARCHAR(500)    NULL,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_SignInLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_SignInLogs PRIMARY KEY (Id)
);
GO
CREATE INDEX IX_SignInLogs_UserId_CreatedAt ON [identity].SignInLogs (UserId, CreatedAt DESC);
GO
CREATE INDEX IX_SignInLogs_EmailAttempted_CreatedAt ON [identity].SignInLogs (EmailAttempted, CreatedAt DESC);
GO
CREATE INDEX IX_SignInLogs_IpAddress_CreatedAt ON [identity].SignInLogs (IpAddress, CreatedAt DESC);
GO
