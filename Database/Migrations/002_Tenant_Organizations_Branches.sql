-- Migration 002: Tenant schema — Organizations, Branches

CREATE TABLE tenant.Organizations (
    Id                   UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Organizations_Id DEFAULT NEWSEQUENTIALID(),
    Name                 NVARCHAR(200)     NOT NULL,
    LegalName            NVARCHAR(200)     NULL,
    Slug                 NVARCHAR(100)     NOT NULL,
    LogoUrl              NVARCHAR(500)     NULL,
    Email                NVARCHAR(256)     NULL,
    Phone                NVARCHAR(30)      NULL,
    Address              NVARCHAR(500)     NULL,
    Status               TINYINT           NOT NULL CONSTRAINT DF_Organizations_Status DEFAULT 0,
    SubscriptionPlanCode NVARCHAR(50)      NULL,
    TrialEndsAt          DATETIME2(3)      NULL,
    CreatedAt            DATETIME2(3)      NOT NULL CONSTRAINT DF_Organizations_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy            UNIQUEIDENTIFIER  NULL,
    UpdatedAt            DATETIME2(3)      NULL,
    UpdatedBy            UNIQUEIDENTIFIER  NULL,
    IsDeleted            BIT               NOT NULL CONSTRAINT DF_Organizations_IsDeleted DEFAULT 0,
    DeletedAt            DATETIME2(3)      NULL,
    DeletedBy            UNIQUEIDENTIFIER  NULL,
    RowVersion           ROWVERSION        NOT NULL,
    CONSTRAINT PK_Organizations PRIMARY KEY NONCLUSTERED (Id),
    CONSTRAINT CK_Organizations_Status CHECK (Status BETWEEN 0 AND 3)
);
GO
CREATE UNIQUE CLUSTERED INDEX CIX_Organizations_CreatedAt ON tenant.Organizations (CreatedAt, Id);
GO
CREATE UNIQUE INDEX UX_Organizations_Slug ON tenant.Organizations (Slug) WHERE IsDeleted = 0;
GO
CREATE INDEX IX_Organizations_Status ON tenant.Organizations (Status) WHERE IsDeleted = 0;
GO

CREATE TABLE tenant.Branches (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Branches_Id DEFAULT NEWSEQUENTIALID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    Name           NVARCHAR(200)    NOT NULL,
    Code           NVARCHAR(50)     NULL,
    Address        NVARCHAR(500)    NULL,
    Phone          NVARCHAR(30)     NULL,
    Email          NVARCHAR(256)    NULL,
    IsMainBranch   BIT              NOT NULL CONSTRAINT DF_Branches_IsMainBranch DEFAULT 0,
    Status         TINYINT          NOT NULL CONSTRAINT DF_Branches_Status DEFAULT 1,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_Branches_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy      UNIQUEIDENTIFIER NULL,
    UpdatedAt      DATETIME2(3)     NULL,
    UpdatedBy      UNIQUEIDENTIFIER NULL,
    IsDeleted      BIT              NOT NULL CONSTRAINT DF_Branches_IsDeleted DEFAULT 0,
    DeletedAt      DATETIME2(3)     NULL,
    DeletedBy      UNIQUEIDENTIFIER NULL,
    RowVersion     ROWVERSION       NOT NULL,
    CONSTRAINT PK_Branches PRIMARY KEY (Id),
    CONSTRAINT FK_Branches_Organizations FOREIGN KEY (OrganizationId)
        REFERENCES tenant.Organizations (Id),
    CONSTRAINT CK_Branches_Status CHECK (Status IN (0, 1))
);
GO
CREATE INDEX IX_Branches_OrganizationId ON tenant.Branches (OrganizationId) WHERE IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Branches_OrganizationId_Code ON tenant.Branches (OrganizationId, Code)
    WHERE Code IS NOT NULL AND IsDeleted = 0;
GO
