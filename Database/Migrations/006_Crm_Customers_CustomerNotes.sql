-- Migration 006: CRM schema — Customers, CustomerNotes

CREATE TABLE crm.Customers (
    Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_Customers_Id DEFAULT NEWSEQUENTIALID(),
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    PersonId       UNIQUEIDENTIFIER NULL,
    CustomerCode   NVARCHAR(50)     NULL,
    CustomerType   NVARCHAR(50)     NOT NULL,   -- vertical-defined label, e.g. 'Student', 'Patient', 'Client'
    DisplayName    NVARCHAR(200)    NOT NULL,
    Status         TINYINT          NOT NULL CONSTRAINT DF_Customers_Status DEFAULT 1,
    Source         NVARCHAR(100)    NULL,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_Customers_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy      UNIQUEIDENTIFIER NULL,
    UpdatedAt      DATETIME2(3)     NULL,
    UpdatedBy      UNIQUEIDENTIFIER NULL,
    IsDeleted      BIT              NOT NULL CONSTRAINT DF_Customers_IsDeleted DEFAULT 0,
    DeletedAt      DATETIME2(3)     NULL,
    DeletedBy      UNIQUEIDENTIFIER NULL,
    RowVersion     ROWVERSION       NOT NULL,
    CONSTRAINT PK_Customers PRIMARY KEY (Id),
    CONSTRAINT FK_Customers_Organizations FOREIGN KEY (OrganizationId) REFERENCES tenant.Organizations (Id),
    CONSTRAINT FK_Customers_Persons FOREIGN KEY (PersonId) REFERENCES [identity].Persons (Id),
    CONSTRAINT CK_Customers_Status CHECK (Status IN (0, 1, 2))
);
GO
CREATE INDEX IX_Customers_OrganizationId_Status ON crm.Customers (OrganizationId, Status) WHERE IsDeleted = 0;
GO
CREATE INDEX IX_Customers_OrganizationId_PersonId ON crm.Customers (OrganizationId, PersonId) WHERE IsDeleted = 0;
GO
CREATE UNIQUE INDEX UX_Customers_OrganizationId_CustomerCode ON crm.Customers (OrganizationId, CustomerCode)
    WHERE CustomerCode IS NOT NULL AND IsDeleted = 0;
GO

CREATE TABLE crm.CustomerNotes (
    Id             BIGINT IDENTITY(1,1) NOT NULL,
    OrganizationId UNIQUEIDENTIFIER NOT NULL,
    CustomerId     UNIQUEIDENTIFIER NOT NULL,
    Note           NVARCHAR(MAX)    NOT NULL,
    CreatedBy      UNIQUEIDENTIFIER NOT NULL,
    CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_CustomerNotes_CreatedAt DEFAULT SYSUTCDATETIME(),
    IsDeleted      BIT              NOT NULL CONSTRAINT DF_CustomerNotes_IsDeleted DEFAULT 0,
    CONSTRAINT PK_CustomerNotes PRIMARY KEY (Id),
    CONSTRAINT FK_CustomerNotes_Customers FOREIGN KEY (CustomerId) REFERENCES crm.Customers (Id),
    CONSTRAINT FK_CustomerNotes_Users FOREIGN KEY (CreatedBy) REFERENCES [identity].Users (Id)
);
GO
CREATE INDEX IX_CustomerNotes_OrganizationId_CustomerId_CreatedAt
    ON crm.CustomerNotes (OrganizationId, CustomerId, CreatedAt DESC) WHERE IsDeleted = 0;
GO
