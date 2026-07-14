-- Migration 007: Audit schema — AuditLogs

CREATE TABLE audit.AuditLogs (
    Id             BIGINT IDENTITY(1,1) NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NULL,   -- NULL for platform-level actions
    UserId         UNIQUEIDENTIFIER NULL,
    Action         NVARCHAR(100)    NOT NULL,
    EntityName     NVARCHAR(150)    NOT NULL,
    EntityId       NVARCHAR(100)    NOT NULL,
    OldValuesJson  NVARCHAR(MAX)    NULL,
    NewValuesJson  NVARCHAR(MAX)    NULL,
    IpAddress      NVARCHAR(45)     NULL,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_AuditLogs_CreatedAt DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_AuditLogs PRIMARY KEY (Id)
);
GO
CREATE INDEX IX_AuditLogs_OrganizationId_CreatedAt ON audit.AuditLogs (OrganizationId, CreatedAt DESC);
GO
CREATE INDEX IX_AuditLogs_EntityName_EntityId ON audit.AuditLogs (EntityName, EntityId);
GO
CREATE INDEX IX_AuditLogs_UserId_CreatedAt ON audit.AuditLogs (UserId, CreatedAt DESC);
GO
