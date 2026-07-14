-- Migration 003: Identity schema — Persons, Users

CREATE TABLE [identity].Persons (
    Id              UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Persons_Id DEFAULT NEWSEQUENTIALID(),
    OrganizationId  UNIQUEIDENTIFIER NOT NULL,
    FirstName       NVARCHAR(100)    NOT NULL,
    LastName        NVARCHAR(100)    NOT NULL,
    FullName        AS (LTRIM(RTRIM(FirstName + N' ' + LastName))) PERSISTED,
    Email           NVARCHAR(256)    NULL,
    Phone           NVARCHAR(30)     NULL,
    DateOfBirth     DATE             NULL,
    Gender          TINYINT          NULL,
    ProfileImageUrl NVARCHAR(500)    NULL,
    CreatedAt       DATETIME2(3)     NOT NULL CONSTRAINT DF_Persons_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy       UNIQUEIDENTIFIER NULL,
    UpdatedAt       DATETIME2(3)     NULL,
    UpdatedBy       UNIQUEIDENTIFIER NULL,
    IsDeleted       BIT              NOT NULL CONSTRAINT DF_Persons_IsDeleted DEFAULT 0,
    DeletedAt       DATETIME2(3)     NULL,
    DeletedBy       UNIQUEIDENTIFIER NULL,
    RowVersion      ROWVERSION       NOT NULL,
    CONSTRAINT PK_Persons PRIMARY KEY (Id),
    CONSTRAINT FK_Persons_Organizations FOREIGN KEY (OrganizationId)
        REFERENCES tenant.Organizations (Id)
);
GO
CREATE INDEX IX_Persons_OrganizationId ON [identity].Persons (OrganizationId) WHERE IsDeleted = 0;
GO
CREATE INDEX IX_Persons_OrganizationId_Email ON [identity].Persons (OrganizationId, Email) WHERE IsDeleted = 0;
GO

CREATE TABLE [identity].Users (
    Id                  UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Users_Id DEFAULT NEWSEQUENTIALID(),
    OrganizationId      UNIQUEIDENTIFIER NOT NULL,
    PersonId            UNIQUEIDENTIFIER NULL,
    Username            NVARCHAR(100)    NOT NULL,
    Email               NVARCHAR(256)    NOT NULL,
    NormalizedEmail     AS (UPPER(Email)) PERSISTED,
    PasswordHash        NVARCHAR(500)    NOT NULL,
    SecurityStamp       NVARCHAR(100)    NOT NULL CONSTRAINT DF_Users_SecurityStamp DEFAULT CONVERT(NVARCHAR(100), NEWID()),
    ConcurrencyStamp    NVARCHAR(100)    NOT NULL CONSTRAINT DF_Users_ConcurrencyStamp DEFAULT CONVERT(NVARCHAR(100), NEWID()),
    IsEmailConfirmed    BIT              NOT NULL CONSTRAINT DF_Users_IsEmailConfirmed DEFAULT 0,
    IsActive            BIT              NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT 1,
    LastLoginAt         DATETIME2(3)     NULL,
    LastLoginIp         NVARCHAR(45)     NULL,
    FailedLoginAttempts INT              NOT NULL CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0,
    LockoutEndDate       DATETIME2(3)     NULL,
    CreatedAt           DATETIME2(3)     NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy           UNIQUEIDENTIFIER NULL,
    UpdatedAt           DATETIME2(3)     NULL,
    UpdatedBy           UNIQUEIDENTIFIER NULL,
    IsDeleted           BIT              NOT NULL CONSTRAINT DF_Users_IsDeleted DEFAULT 0,
    DeletedAt           DATETIME2(3)     NULL,
    DeletedBy           UNIQUEIDENTIFIER NULL,
    RowVersion          ROWVERSION       NOT NULL,
    CONSTRAINT PK_Users PRIMARY KEY (Id),
    CONSTRAINT FK_Users_Organizations FOREIGN KEY (OrganizationId)
        REFERENCES tenant.Organizations (Id),
    CONSTRAINT FK_Users_Persons FOREIGN KEY (PersonId)
        REFERENCES [identity].Persons (Id)
);
GO
CREATE INDEX IX_Users_OrganizationId ON [identity].Users (OrganizationId) WHERE IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Users_OrganizationId_NormalizedEmail ON [identity].Users (OrganizationId, NormalizedEmail) WHERE IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Users_OrganizationId_Username ON [identity].Users (OrganizationId, Username) WHERE IsDeleted = 0;
GO
CREATE INDEX IX_Users_LockoutEndDate ON [identity].Users (LockoutEndDate) WHERE LockoutEndDate IS NOT NULL;
GO
