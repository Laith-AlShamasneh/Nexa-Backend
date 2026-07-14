-- Migration 004: Identity schema — Roles, Permissions, RolePermissions, UserRoles

CREATE TABLE [identity].Roles (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Roles_Id DEFAULT NEWSEQUENTIALID(),
    OrganizationId UNIQUEIDENTIFIER NULL,   -- NULL = system role template shared by all tenants
    Name           NVARCHAR(100)    NOT NULL,
    Description    NVARCHAR(500)    NULL,
    IsSystemRole   BIT              NOT NULL CONSTRAINT DF_Roles_IsSystemRole DEFAULT 0,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_Roles_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy      UNIQUEIDENTIFIER NULL,
    UpdatedAt      DATETIME2(3)     NULL,
    UpdatedBy      UNIQUEIDENTIFIER NULL,
    IsDeleted      BIT              NOT NULL CONSTRAINT DF_Roles_IsDeleted DEFAULT 0,
    CONSTRAINT PK_Roles PRIMARY KEY (Id),
    CONSTRAINT FK_Roles_Organizations FOREIGN KEY (OrganizationId)
        REFERENCES tenant.Organizations (Id)
);
GO
CREATE INDEX IX_Roles_OrganizationId ON [identity].Roles (OrganizationId) WHERE IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Roles_Name_System ON [identity].Roles (Name) WHERE OrganizationId IS NULL AND IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Roles_OrganizationId_Name ON [identity].Roles (OrganizationId, Name) WHERE OrganizationId IS NOT NULL AND IsDeleted = 0;
GO

CREATE TABLE [identity].Permissions (
    Id          INT IDENTITY(1,1) NOT NULL,
    Code        NVARCHAR(150)     NOT NULL,
    Name        NVARCHAR(200)     NOT NULL,
    Description NVARCHAR(500)     NULL,
    Module      NVARCHAR(100)     NULL,
    CreatedAt   DATETIME2(3)      NOT NULL CONSTRAINT DF_Permissions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_Permissions PRIMARY KEY (Id)
);
GO
CREATE UNIQUE INDEX UX_Permissions_Code ON [identity].Permissions (Code);
GO

CREATE TABLE [identity].RolePermissions (
    RoleId       UNIQUEIDENTIFIER NOT NULL,
    PermissionId INT              NOT NULL,
    CreatedAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_RolePermissions_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_RolePermissions PRIMARY KEY (RoleId, PermissionId),
    CONSTRAINT FK_RolePermissions_Roles FOREIGN KEY (RoleId) REFERENCES [identity].Roles (Id),
    CONSTRAINT FK_RolePermissions_Permissions FOREIGN KEY (PermissionId) REFERENCES [identity].Permissions (Id)
);
GO
CREATE INDEX IX_RolePermissions_PermissionId ON [identity].RolePermissions (PermissionId);
GO

CREATE TABLE [identity].UserRoles (
    UserId         UNIQUEIDENTIFIER NOT NULL,
    RoleId         UNIQUEIDENTIFIER NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt     DATETIME2(3)     NOT NULL CONSTRAINT DF_UserRoles_AssignedAt DEFAULT SYSUTCDATETIME(),
    AssignedBy     UNIQUEIDENTIFIER NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES [identity].Users (Id),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES [identity].Roles (Id)
);
GO
CREATE INDEX IX_UserRoles_RoleId ON [identity].UserRoles (RoleId);
GO
CREATE INDEX IX_UserRoles_OrganizationId ON [identity].UserRoles (OrganizationId);
GO
