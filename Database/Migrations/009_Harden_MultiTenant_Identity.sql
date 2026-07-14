/*
    Migration 009: Harden Multi-Tenant Identity and Core Foundation

    Applies security, tenant-isolation, integrity, indexing, token, audit,
    settings, invitation, and session-management improvements to migrations
    001-008.

    IMPORTANT:
    - Run this after 001-008.
    - The migration validates existing data before adding stricter constraints.
    - It is designed to be re-runnable where practical.
    - It intentionally does not rebuild the clustered index on tenant.Organizations;
      that physical-design change should be performed separately after measuring
      production table/index size and deployment impact.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    ---------------------------------------------------------------------------
    -- 0. Migration history
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.SchemaVersions
        (
            MigrationId NVARCHAR(100) NOT NULL,
            [Name]      NVARCHAR(250) NOT NULL,
            AppliedAt   DATETIME2(3)  NOT NULL CONSTRAINT DF_SchemaVersions_AppliedAt DEFAULT SYSUTCDATETIME(),
            [Checksum]  CHAR(64)      NULL,
            CONSTRAINT PK_SchemaVersions PRIMARY KEY (MigrationId)
        );
    END;

    IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'009')
    BEGIN
        COMMIT TRANSACTION;
        RETURN;
    END;

    ---------------------------------------------------------------------------
    -- 1. Validate existing data before tenant-hardening
    ---------------------------------------------------------------------------
    IF EXISTS
    (
        SELECT 1
        FROM [identity].Users u
        INNER JOIN [identity].Persons p ON p.Id = u.PersonId
        WHERE u.PersonId IS NOT NULL
          AND u.OrganizationId <> p.OrganizationId
    )
        THROW 51001, 'Migration 009 stopped: Users contain cross-tenant Person references.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM crm.Customers c
        INNER JOIN [identity].Persons p ON p.Id = c.PersonId
        WHERE c.PersonId IS NOT NULL
          AND c.OrganizationId <> p.OrganizationId
    )
        THROW 51002, 'Migration 009 stopped: Customers contain cross-tenant Person references.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM crm.CustomerNotes n
        INNER JOIN crm.Customers c ON c.Id = n.CustomerId
        WHERE n.OrganizationId <> c.OrganizationId
    )
        THROW 51003, 'Migration 009 stopped: CustomerNotes contain cross-tenant Customer references.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM crm.CustomerNotes n
        INNER JOIN [identity].Users u ON u.Id = n.CreatedBy
        WHERE n.OrganizationId <> u.OrganizationId
    )
        THROW 51004, 'Migration 009 stopped: CustomerNotes contain cross-tenant CreatedBy references.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [identity].RefreshTokens t
        INNER JOIN [identity].Users u ON u.Id = t.UserId
        WHERE t.OrganizationId <> u.OrganizationId
    )
        THROW 51005, 'Migration 009 stopped: RefreshTokens contain cross-tenant User references.', 1;

    IF EXISTS
    (
        SELECT OrganizationId, PersonId
        FROM [identity].Users
        WHERE PersonId IS NOT NULL AND IsDeleted = 0
        GROUP BY OrganizationId, PersonId
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51006, 'Migration 009 stopped: more than one active User exists for the same Person within a tenant.', 1;

    IF EXISTS
    (
        SELECT OrganizationId
        FROM tenant.Branches
        WHERE IsMainBranch = 1 AND IsDeleted = 0
        GROUP BY OrganizationId
        HAVING COUNT_BIG(*) > 1
    )
        THROW 51007, 'Migration 009 stopped: an Organization has more than one active main Branch.', 1;

    ---------------------------------------------------------------------------
    -- 2. Tenant and branch integrity
    ---------------------------------------------------------------------------
    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'tenant.Organizations')
          AND name = N'CK_Organizations_TrialEndsAt'
    )
    BEGIN
        ALTER TABLE tenant.Organizations WITH CHECK
        ADD CONSTRAINT CK_Organizations_TrialEndsAt
            CHECK (TrialEndsAt IS NULL OR TrialEndsAt >= CreatedAt);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'tenant.Organizations')
          AND name = N'CK_Organizations_SoftDelete'
    )
    BEGIN
        ALTER TABLE tenant.Organizations WITH CHECK
        ADD CONSTRAINT CK_Organizations_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'tenant.Branches')
          AND name = N'CK_Branches_SoftDelete'
    )
    BEGIN
        ALTER TABLE tenant.Branches WITH CHECK
        ADD CONSTRAINT CK_Branches_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'tenant.Branches') AND name = N'UX_Branches_Organization_MainBranch')
    BEGIN
        CREATE UNIQUE INDEX UX_Branches_Organization_MainBranch
            ON tenant.Branches (OrganizationId)
            WHERE IsMainBranch = 1 AND IsDeleted = 0;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'tenant.Branches') AND name = N'UX_Branches_OrganizationId_Name')
    BEGIN
        CREATE UNIQUE INDEX UX_Branches_OrganizationId_Name
            ON tenant.Branches (OrganizationId, Name)
            WHERE IsDeleted = 0;
    END;

    ---------------------------------------------------------------------------
    -- 3. Organization settings
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'tenant.OrganizationSettings', N'U') IS NULL
    BEGIN
        CREATE TABLE tenant.OrganizationSettings
        (
            OrganizationId UNIQUEIDENTIFIER NOT NULL,
            TimeZoneId     NVARCHAR(100)    NOT NULL CONSTRAINT DF_OrganizationSettings_TimeZoneId DEFAULT N'Asia/Amman',
            DefaultLanguageCode NVARCHAR(10) NOT NULL CONSTRAINT DF_OrganizationSettings_DefaultLanguage DEFAULT N'ar-JO',
            CurrencyCode   CHAR(3)          NOT NULL CONSTRAINT DF_OrganizationSettings_CurrencyCode DEFAULT 'JOD',
            DateFormat     NVARCHAR(30)     NOT NULL CONSTRAINT DF_OrganizationSettings_DateFormat DEFAULT N'dd/MM/yyyy',
            ReceiptPrefix  NVARCHAR(20)     NULL,
            AdditionalSettingsJson NVARCHAR(MAX) NULL,
            CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_OrganizationSettings_CreatedAt DEFAULT SYSUTCDATETIME(),
            CreatedBy      UNIQUEIDENTIFIER NULL,
            UpdatedAt      DATETIME2(3)     NULL,
            UpdatedBy      UNIQUEIDENTIFIER NULL,
            RowVersion     ROWVERSION       NOT NULL,
            CONSTRAINT PK_OrganizationSettings PRIMARY KEY (OrganizationId),
            CONSTRAINT FK_OrganizationSettings_Organizations FOREIGN KEY (OrganizationId)
                REFERENCES tenant.Organizations(Id),
            CONSTRAINT CK_OrganizationSettings_AdditionalJson CHECK
                (AdditionalSettingsJson IS NULL OR ISJSON(AdditionalSettingsJson) = 1)
        );
    END;

    INSERT INTO tenant.OrganizationSettings (OrganizationId)
    SELECT o.Id
    FROM tenant.Organizations o
    WHERE o.IsDeleted = 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM tenant.OrganizationSettings s
          WHERE s.OrganizationId = o.Id
      );

    ---------------------------------------------------------------------------
    -- 4. Persons and Users normalization / integrity
    ---------------------------------------------------------------------------
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Persons') AND name = N'UX_Persons_OrganizationId_Id')
    BEGIN
        CREATE UNIQUE INDEX UX_Persons_OrganizationId_Id
            ON [identity].Persons (OrganizationId, Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].Persons')
          AND name = N'CK_Persons_SoftDelete'
    )
    BEGIN
        ALTER TABLE [identity].Persons WITH CHECK
        ADD CONSTRAINT CK_Persons_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    -- Rebuild normalized e-mail expression with trimming.
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Users') AND name = N'UX_Users_OrganizationId_NormalizedEmail')
        DROP INDEX UX_Users_OrganizationId_NormalizedEmail ON [identity].Users;

    IF COL_LENGTH(N'identity.Users', N'NormalizedEmail') IS NOT NULL
        ALTER TABLE [identity].Users DROP COLUMN NormalizedEmail;

    ALTER TABLE [identity].Users
        ADD NormalizedEmail AS (UPPER(LTRIM(RTRIM(Email)))) PERSISTED;

    IF COL_LENGTH(N'identity.Users', N'NormalizedUsername') IS NULL
    BEGIN
        ALTER TABLE [identity].Users
            ADD NormalizedUsername AS (UPPER(LTRIM(RTRIM(Username)))) PERSISTED;
    END;

    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Users') AND name = N'UX_Users_OrganizationId_Username')
        DROP INDEX UX_Users_OrganizationId_Username ON [identity].Users;

    CREATE UNIQUE INDEX UX_Users_OrganizationId_NormalizedEmail
        ON [identity].Users (OrganizationId, NormalizedEmail)
        WHERE IsDeleted = 0;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Users') AND name = N'UX_Users_OrganizationId_NormalizedUsername')
    BEGIN
        CREATE UNIQUE INDEX UX_Users_OrganizationId_NormalizedUsername
            ON [identity].Users (OrganizationId, NormalizedUsername)
            WHERE IsDeleted = 0;
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Users') AND name = N'UX_Users_OrganizationId_Id')
    BEGIN
        CREATE UNIQUE INDEX UX_Users_OrganizationId_Id
            ON [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Users') AND name = N'UX_Users_OrganizationId_PersonId')
    BEGIN
        CREATE UNIQUE INDEX UX_Users_OrganizationId_PersonId
            ON [identity].Users (OrganizationId, PersonId)
            WHERE PersonId IS NOT NULL AND IsDeleted = 0;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].Users')
          AND name = N'CK_Users_FailedLoginAttempts'
    )
    BEGIN
        ALTER TABLE [identity].Users WITH CHECK
        ADD CONSTRAINT CK_Users_FailedLoginAttempts CHECK (FailedLoginAttempts >= 0);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].Users')
          AND name = N'CK_Users_SoftDelete'
    )
    BEGIN
        ALTER TABLE [identity].Users WITH CHECK
        ADD CONSTRAINT CK_Users_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].Users') AND name = N'FK_Users_Persons')
        ALTER TABLE [identity].Users DROP CONSTRAINT FK_Users_Persons;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].Users') AND name = N'FK_Users_Persons_Tenant')
    BEGIN
        ALTER TABLE [identity].Users WITH CHECK
        ADD CONSTRAINT FK_Users_Persons_Tenant
            FOREIGN KEY (OrganizationId, PersonId)
            REFERENCES [identity].Persons (OrganizationId, Id);
    END;

    ---------------------------------------------------------------------------
    -- 5. Role templates, tenant roles, and tenant-safe assignments
    ---------------------------------------------------------------------------
    IF COL_LENGTH(N'identity.Roles', N'TemplateRoleId') IS NULL
        ALTER TABLE [identity].Roles ADD TemplateRoleId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'identity.Roles', N'DeletedAt') IS NULL
        ALTER TABLE [identity].Roles ADD DeletedAt DATETIME2(3) NULL;

    IF COL_LENGTH(N'identity.Roles', N'DeletedBy') IS NULL
        ALTER TABLE [identity].Roles ADD DeletedBy UNIQUEIDENTIFIER NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].Roles') AND name = N'FK_Roles_TemplateRole')
    BEGIN
        ALTER TABLE [identity].Roles WITH CHECK
        ADD CONSTRAINT FK_Roles_TemplateRole
            FOREIGN KEY (TemplateRoleId) REFERENCES [identity].Roles(Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Roles') AND name = N'UX_Roles_OrganizationId_Id')
    BEGIN
        CREATE UNIQUE INDEX UX_Roles_OrganizationId_Id
            ON [identity].Roles (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].Roles') AND name = N'UX_Roles_OrganizationId_TemplateRoleId')
    BEGIN
        CREATE UNIQUE INDEX UX_Roles_OrganizationId_TemplateRoleId
            ON [identity].Roles (OrganizationId, TemplateRoleId)
            WHERE OrganizationId IS NOT NULL AND TemplateRoleId IS NOT NULL AND IsDeleted = 0;
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].Roles')
          AND name = N'CK_Roles_TemplateScope'
    )
    BEGIN
        ALTER TABLE [identity].Roles WITH CHECK
        ADD CONSTRAINT CK_Roles_TemplateScope
            CHECK
            (
                (OrganizationId IS NULL AND TemplateRoleId IS NULL)
                OR
                (OrganizationId IS NOT NULL)
            );
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].Roles')
          AND name = N'CK_Roles_SoftDelete'
    )
    BEGIN
        ALTER TABLE [identity].Roles WITH CHECK
        ADD CONSTRAINT CK_Roles_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    -- Give Admin an explicit permission set (all except owner-only organization management).
    INSERT INTO [identity].RolePermissions (RoleId, PermissionId)
    SELECT r.Id, p.Id
    FROM [identity].Roles r
    CROSS JOIN [identity].Permissions p
    WHERE r.Name = N'Admin'
      AND r.OrganizationId IS NULL
      AND p.Code <> N'Organization.Manage'
      AND NOT EXISTS
      (
          SELECT 1
          FROM [identity].RolePermissions rp
          WHERE rp.RoleId = r.Id
            AND rp.PermissionId = p.Id
      );

    -- Materialize tenant-local role copies from global templates.
    INSERT INTO [identity].Roles
    (
        OrganizationId, TemplateRoleId, Name, Description, IsSystemRole,
        CreatedAt, IsDeleted
    )
    SELECT
        o.Id, t.Id, t.Name, t.Description, 1,
        SYSUTCDATETIME(), 0
    FROM tenant.Organizations o
    CROSS JOIN [identity].Roles t
    WHERE o.IsDeleted = 0
      AND t.OrganizationId IS NULL
      AND t.IsSystemRole = 1
      AND t.IsDeleted = 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM [identity].Roles tr
          WHERE tr.OrganizationId = o.Id
            AND tr.TemplateRoleId = t.Id
            AND tr.IsDeleted = 0
      );

    INSERT INTO [identity].RolePermissions (RoleId, PermissionId)
    SELECT tenantRole.Id, templatePermission.PermissionId
    FROM [identity].Roles tenantRole
    INNER JOIN [identity].RolePermissions templatePermission
        ON templatePermission.RoleId = tenantRole.TemplateRoleId
    WHERE tenantRole.OrganizationId IS NOT NULL
      AND tenantRole.IsDeleted = 0
      AND NOT EXISTS
      (
          SELECT 1
          FROM [identity].RolePermissions existing
          WHERE existing.RoleId = tenantRole.Id
            AND existing.PermissionId = templatePermission.PermissionId
      );

    -- Convert any existing global-template assignments to tenant-local roles.
    UPDATE ur
        SET ur.RoleId = tenantRole.Id
    FROM [identity].UserRoles ur
    INNER JOIN [identity].Roles globalRole
        ON globalRole.Id = ur.RoleId
       AND globalRole.OrganizationId IS NULL
    INNER JOIN [identity].Roles tenantRole
        ON tenantRole.OrganizationId = ur.OrganizationId
       AND tenantRole.TemplateRoleId = globalRole.Id
       AND tenantRole.IsDeleted = 0;

    IF EXISTS
    (
        SELECT 1
        FROM [identity].UserRoles ur
        INNER JOIN [identity].Users u ON u.Id = ur.UserId
        WHERE ur.OrganizationId <> u.OrganizationId
    )
        THROW 51008, 'Migration 009 stopped: UserRoles contain a User from another tenant.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [identity].UserRoles ur
        INNER JOIN [identity].Roles r ON r.Id = ur.RoleId
        WHERE r.OrganizationId IS NULL OR ur.OrganizationId <> r.OrganizationId
    )
        THROW 51009, 'Migration 009 stopped: UserRoles must reference tenant-local Roles.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM [identity].UserRoles ur
        INNER JOIN [identity].Users u ON u.Id = ur.AssignedBy
        WHERE ur.AssignedBy IS NOT NULL
          AND ur.OrganizationId <> u.OrganizationId
    )
        THROW 51010, 'Migration 009 stopped: UserRoles contain cross-tenant AssignedBy references.', 1;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].UserRoles') AND name = N'FK_UserRoles_Users')
        ALTER TABLE [identity].UserRoles DROP CONSTRAINT FK_UserRoles_Users;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].UserRoles') AND name = N'FK_UserRoles_Roles')
        ALTER TABLE [identity].UserRoles DROP CONSTRAINT FK_UserRoles_Roles;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].UserRoles') AND name = N'FK_UserRoles_Users_Tenant')
    BEGIN
        ALTER TABLE [identity].UserRoles WITH CHECK
        ADD CONSTRAINT FK_UserRoles_Users_Tenant
            FOREIGN KEY (OrganizationId, UserId)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].UserRoles') AND name = N'FK_UserRoles_Roles_Tenant')
    BEGIN
        ALTER TABLE [identity].UserRoles WITH CHECK
        ADD CONSTRAINT FK_UserRoles_Roles_Tenant
            FOREIGN KEY (OrganizationId, RoleId)
            REFERENCES [identity].Roles (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].UserRoles') AND name = N'FK_UserRoles_AssignedBy_Tenant')
    BEGIN
        ALTER TABLE [identity].UserRoles WITH CHECK
        ADD CONSTRAINT FK_UserRoles_AssignedBy_Tenant
            FOREIGN KEY (OrganizationId, AssignedBy)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    ---------------------------------------------------------------------------
    -- 6. Authentication token hardening
    ---------------------------------------------------------------------------
    IF COL_LENGTH(N'identity.RefreshTokens', N'ReplacedByTokenId') IS NULL
        ALTER TABLE [identity].RefreshTokens ADD ReplacedByTokenId BIGINT NULL;

    IF COL_LENGTH(N'identity.RefreshTokens', N'TokenFamilyId') IS NULL
        ALTER TABLE [identity].RefreshTokens ADD TokenFamilyId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_RefreshTokens_TokenFamilyId DEFAULT NEWID();

    IF COL_LENGTH(N'identity.RefreshTokens', N'SessionId') IS NULL
        ALTER TABLE [identity].RefreshTokens ADD SessionId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'identity.RefreshTokens', N'RevocationReason') IS NULL
        ALTER TABLE [identity].RefreshTokens ADD RevocationReason NVARCHAR(250) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].RefreshTokens') AND name = N'FK_RefreshTokens_ReplacedByToken')
    BEGIN
        ALTER TABLE [identity].RefreshTokens WITH CHECK
        ADD CONSTRAINT FK_RefreshTokens_ReplacedByToken
            FOREIGN KEY (ReplacedByTokenId) REFERENCES [identity].RefreshTokens(Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].RefreshTokens')
          AND name = N'CK_RefreshTokens_Expiry'
    )
    BEGIN
        ALTER TABLE [identity].RefreshTokens WITH CHECK
        ADD CONSTRAINT CK_RefreshTokens_Expiry CHECK (ExpiresAt > CreatedAt);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].RefreshTokens')
          AND name = N'CK_RefreshTokens_Revocation'
    )
    BEGIN
        ALTER TABLE [identity].RefreshTokens WITH CHECK
        ADD CONSTRAINT CK_RefreshTokens_Revocation
            CHECK (RevokedAt IS NULL OR RevokedAt >= CreatedAt);
    END;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].RefreshTokens') AND name = N'FK_RefreshTokens_Users')
        ALTER TABLE [identity].RefreshTokens DROP CONSTRAINT FK_RefreshTokens_Users;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].RefreshTokens') AND name = N'FK_RefreshTokens_Users_Tenant')
    BEGIN
        ALTER TABLE [identity].RefreshTokens WITH CHECK
        ADD CONSTRAINT FK_RefreshTokens_Users_Tenant
            FOREIGN KEY (OrganizationId, UserId)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].RefreshTokens') AND name = N'IX_RefreshTokens_UserId_ExpiresAt')
    BEGIN
        CREATE INDEX IX_RefreshTokens_UserId_ExpiresAt
            ON [identity].RefreshTokens (UserId, ExpiresAt)
            INCLUDE (OrganizationId, RevokedAt, TokenFamilyId, SessionId);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].RefreshTokens') AND name = N'IX_RefreshTokens_TokenFamilyId')
    BEGIN
        CREATE INDEX IX_RefreshTokens_TokenFamilyId
            ON [identity].RefreshTokens (TokenFamilyId, CreatedAt);
    END;

    -- Email-confirmation tokens: add tenant isolation and revocation.
    IF COL_LENGTH(N'identity.EmailConfirmationTokens', N'OrganizationId') IS NULL
        ALTER TABLE [identity].EmailConfirmationTokens ADD OrganizationId UNIQUEIDENTIFIER NULL;

    UPDATE t
       SET OrganizationId = u.OrganizationId
    FROM [identity].EmailConfirmationTokens t
    INNER JOIN [identity].Users u ON u.Id = t.UserId
    WHERE t.OrganizationId IS NULL;

    IF EXISTS (SELECT 1 FROM [identity].EmailConfirmationTokens WHERE OrganizationId IS NULL)
        THROW 51011, 'Migration 009 stopped: EmailConfirmationTokens could not be mapped to a tenant.', 1;

    ALTER TABLE [identity].EmailConfirmationTokens ALTER COLUMN OrganizationId UNIQUEIDENTIFIER NOT NULL;

    IF COL_LENGTH(N'identity.EmailConfirmationTokens', N'RevokedAt') IS NULL
        ALTER TABLE [identity].EmailConfirmationTokens ADD RevokedAt DATETIME2(3) NULL;

    IF COL_LENGTH(N'identity.EmailConfirmationTokens', N'RevocationReason') IS NULL
        ALTER TABLE [identity].EmailConfirmationTokens ADD RevocationReason NVARCHAR(250) NULL;

    IF COL_LENGTH(N'identity.EmailConfirmationTokens', N'CreatedByIp') IS NULL
        ALTER TABLE [identity].EmailConfirmationTokens ADD CreatedByIp NVARCHAR(45) NULL;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].EmailConfirmationTokens') AND name = N'FK_EmailConfirmationTokens_Users')
        ALTER TABLE [identity].EmailConfirmationTokens DROP CONSTRAINT FK_EmailConfirmationTokens_Users;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].EmailConfirmationTokens') AND name = N'FK_EmailConfirmationTokens_Users_Tenant')
    BEGIN
        ALTER TABLE [identity].EmailConfirmationTokens WITH CHECK
        ADD CONSTRAINT FK_EmailConfirmationTokens_Users_Tenant
            FOREIGN KEY (OrganizationId, UserId)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].EmailConfirmationTokens')
          AND name = N'CK_EmailConfirmationTokens_Lifecycle'
    )
    BEGIN
        ALTER TABLE [identity].EmailConfirmationTokens WITH CHECK
        ADD CONSTRAINT CK_EmailConfirmationTokens_Lifecycle CHECK
        (
            ExpiresAt > CreatedAt
            AND (UsedAt IS NULL OR UsedAt >= CreatedAt)
            AND (RevokedAt IS NULL OR RevokedAt >= CreatedAt)
            AND NOT (UsedAt IS NOT NULL AND RevokedAt IS NOT NULL)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].EmailConfirmationTokens') AND name = N'IX_EmailConfirmationTokens_OrganizationId_UserId_ExpiresAt')
    BEGIN
        CREATE INDEX IX_EmailConfirmationTokens_OrganizationId_UserId_ExpiresAt
            ON [identity].EmailConfirmationTokens (OrganizationId, UserId, ExpiresAt)
            INCLUDE (UsedAt, RevokedAt);
    END;

    -- Password-reset tokens: add tenant isolation and revocation.
    IF COL_LENGTH(N'identity.PasswordResetTokens', N'OrganizationId') IS NULL
        ALTER TABLE [identity].PasswordResetTokens ADD OrganizationId UNIQUEIDENTIFIER NULL;

    UPDATE t
       SET OrganizationId = u.OrganizationId
    FROM [identity].PasswordResetTokens t
    INNER JOIN [identity].Users u ON u.Id = t.UserId
    WHERE t.OrganizationId IS NULL;

    IF EXISTS (SELECT 1 FROM [identity].PasswordResetTokens WHERE OrganizationId IS NULL)
        THROW 51012, 'Migration 009 stopped: PasswordResetTokens could not be mapped to a tenant.', 1;

    ALTER TABLE [identity].PasswordResetTokens ALTER COLUMN OrganizationId UNIQUEIDENTIFIER NOT NULL;

    IF COL_LENGTH(N'identity.PasswordResetTokens', N'RevokedAt') IS NULL
        ALTER TABLE [identity].PasswordResetTokens ADD RevokedAt DATETIME2(3) NULL;

    IF COL_LENGTH(N'identity.PasswordResetTokens', N'RevocationReason') IS NULL
        ALTER TABLE [identity].PasswordResetTokens ADD RevocationReason NVARCHAR(250) NULL;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].PasswordResetTokens') AND name = N'FK_PasswordResetTokens_Users')
        ALTER TABLE [identity].PasswordResetTokens DROP CONSTRAINT FK_PasswordResetTokens_Users;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'[identity].PasswordResetTokens') AND name = N'FK_PasswordResetTokens_Users_Tenant')
    BEGIN
        ALTER TABLE [identity].PasswordResetTokens WITH CHECK
        ADD CONSTRAINT FK_PasswordResetTokens_Users_Tenant
            FOREIGN KEY (OrganizationId, UserId)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'[identity].PasswordResetTokens')
          AND name = N'CK_PasswordResetTokens_Lifecycle'
    )
    BEGIN
        ALTER TABLE [identity].PasswordResetTokens WITH CHECK
        ADD CONSTRAINT CK_PasswordResetTokens_Lifecycle CHECK
        (
            ExpiresAt > CreatedAt
            AND (UsedAt IS NULL OR UsedAt >= CreatedAt)
            AND (RevokedAt IS NULL OR RevokedAt >= CreatedAt)
            AND NOT (UsedAt IS NOT NULL AND RevokedAt IS NOT NULL)
        );
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].PasswordResetTokens') AND name = N'IX_PasswordResetTokens_OrganizationId_UserId_ExpiresAt')
    BEGIN
        CREATE INDEX IX_PasswordResetTokens_OrganizationId_UserId_ExpiresAt
            ON [identity].PasswordResetTokens (OrganizationId, UserId, ExpiresAt)
            INCLUDE (UsedAt, RevokedAt);
    END;

    ---------------------------------------------------------------------------
    -- 7. User sessions and invitations
    ---------------------------------------------------------------------------
    IF OBJECT_ID(N'[identity].UserSessions', N'U') IS NULL
    BEGIN
        CREATE TABLE [identity].UserSessions
        (
            Id             UNIQUEIDENTIFIER NOT NULL CONSTRAINT DF_UserSessions_Id DEFAULT NEWSEQUENTIALID(),
            OrganizationId UNIQUEIDENTIFIER NOT NULL,
            UserId         UNIQUEIDENTIFIER NOT NULL,
            DeviceId       NVARCHAR(200)    NULL,
            DeviceName     NVARCHAR(200)    NULL,
            UserAgent      NVARCHAR(500)    NULL,
            IpAddress      NVARCHAR(45)     NULL,
            CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_UserSessions_CreatedAt DEFAULT SYSUTCDATETIME(),
            LastSeenAt     DATETIME2(3)     NOT NULL CONSTRAINT DF_UserSessions_LastSeenAt DEFAULT SYSUTCDATETIME(),
            ExpiresAt      DATETIME2(3)     NULL,
            RevokedAt      DATETIME2(3)     NULL,
            RevokedBy      UNIQUEIDENTIFIER NULL,
            RevocationReason NVARCHAR(250)  NULL,
            CONSTRAINT PK_UserSessions PRIMARY KEY (Id),
            CONSTRAINT FK_UserSessions_Users_Tenant FOREIGN KEY (OrganizationId, UserId)
                REFERENCES [identity].Users (OrganizationId, Id),
            CONSTRAINT FK_UserSessions_RevokedBy_Tenant FOREIGN KEY (OrganizationId, RevokedBy)
                REFERENCES [identity].Users (OrganizationId, Id),
            CONSTRAINT CK_UserSessions_Lifecycle CHECK
            (
                (ExpiresAt IS NULL OR ExpiresAt > CreatedAt)
                AND (RevokedAt IS NULL OR RevokedAt >= CreatedAt)
            )
        );

        CREATE INDEX IX_UserSessions_OrganizationId_UserId_LastSeenAt
            ON [identity].UserSessions (OrganizationId, UserId, LastSeenAt DESC)
            INCLUDE (RevokedAt, ExpiresAt);
    END;

    IF OBJECT_ID(N'[identity].UserInvitations', N'U') IS NULL
    BEGIN
        CREATE TABLE [identity].UserInvitations
        (
            Id             BIGINT IDENTITY(1,1) NOT NULL,
            OrganizationId UNIQUEIDENTIFIER NOT NULL,
            Email          NVARCHAR(256)    NOT NULL,
            NormalizedEmail AS (UPPER(LTRIM(RTRIM(Email)))) PERSISTED,
            RoleId         UNIQUEIDENTIFIER NOT NULL,
            TokenHash      CHAR(64)         NOT NULL,
            InvitedBy      UNIQUEIDENTIFIER NOT NULL,
            ExpiresAt      DATETIME2(3)     NOT NULL,
            AcceptedAt     DATETIME2(3)     NULL,
            RevokedAt      DATETIME2(3)     NULL,
            RevocationReason NVARCHAR(250)  NULL,
            CreatedAt      DATETIME2(3)     NOT NULL CONSTRAINT DF_UserInvitations_CreatedAt DEFAULT SYSUTCDATETIME(),
            CONSTRAINT PK_UserInvitations PRIMARY KEY (Id),
            CONSTRAINT FK_UserInvitations_Organizations FOREIGN KEY (OrganizationId)
                REFERENCES tenant.Organizations(Id),
            CONSTRAINT FK_UserInvitations_Roles_Tenant FOREIGN KEY (OrganizationId, RoleId)
                REFERENCES [identity].Roles (OrganizationId, Id),
            CONSTRAINT FK_UserInvitations_InvitedBy_Tenant FOREIGN KEY (OrganizationId, InvitedBy)
                REFERENCES [identity].Users (OrganizationId, Id),
            CONSTRAINT CK_UserInvitations_Lifecycle CHECK
            (
                ExpiresAt > CreatedAt
                AND (AcceptedAt IS NULL OR AcceptedAt >= CreatedAt)
                AND (RevokedAt IS NULL OR RevokedAt >= CreatedAt)
                AND NOT (AcceptedAt IS NOT NULL AND RevokedAt IS NOT NULL)
            )
        );

        CREATE UNIQUE INDEX UX_UserInvitations_TokenHash
            ON [identity].UserInvitations(TokenHash);

        CREATE INDEX IX_UserInvitations_OrganizationId_NormalizedEmail_ExpiresAt
            ON [identity].UserInvitations(OrganizationId, NormalizedEmail, ExpiresAt)
            INCLUDE (AcceptedAt, RevokedAt, RoleId);
    END;

    ---------------------------------------------------------------------------
    -- 8. CRM tenant integrity and cleanup
    ---------------------------------------------------------------------------
    ALTER TABLE crm.Customers ALTER COLUMN DisplayName NVARCHAR(200) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'crm.Customers') AND name = N'UX_Customers_OrganizationId_Id')
    BEGIN
        CREATE UNIQUE INDEX UX_Customers_OrganizationId_Id
            ON crm.Customers (OrganizationId, Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'crm.Customers')
          AND name = N'CK_Customers_CustomerType_NotBlank'
    )
    BEGIN
        ALTER TABLE crm.Customers WITH CHECK
        ADD CONSTRAINT CK_Customers_CustomerType_NotBlank
            CHECK (LEN(LTRIM(RTRIM(CustomerType))) > 0);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'crm.Customers')
          AND name = N'CK_Customers_SoftDelete'
    )
    BEGIN
        ALTER TABLE crm.Customers WITH CHECK
        ADD CONSTRAINT CK_Customers_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.Customers') AND name = N'FK_Customers_Persons')
        ALTER TABLE crm.Customers DROP CONSTRAINT FK_Customers_Persons;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.Customers') AND name = N'FK_Customers_Persons_Tenant')
    BEGIN
        ALTER TABLE crm.Customers WITH CHECK
        ADD CONSTRAINT FK_Customers_Persons_Tenant
            FOREIGN KEY (OrganizationId, PersonId)
            REFERENCES [identity].Persons (OrganizationId, Id);
    END;

    IF COL_LENGTH(N'crm.CustomerNotes', N'DeletedAt') IS NULL
        ALTER TABLE crm.CustomerNotes ADD DeletedAt DATETIME2(3) NULL;

    IF COL_LENGTH(N'crm.CustomerNotes', N'DeletedBy') IS NULL
        ALTER TABLE crm.CustomerNotes ADD DeletedBy UNIQUEIDENTIFIER NULL;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes') AND name = N'FK_CustomerNotes_Customers')
        ALTER TABLE crm.CustomerNotes DROP CONSTRAINT FK_CustomerNotes_Customers;

    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes') AND name = N'FK_CustomerNotes_Users')
        ALTER TABLE crm.CustomerNotes DROP CONSTRAINT FK_CustomerNotes_Users;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes') AND name = N'FK_CustomerNotes_Customers_Tenant')
    BEGIN
        ALTER TABLE crm.CustomerNotes WITH CHECK
        ADD CONSTRAINT FK_CustomerNotes_Customers_Tenant
            FOREIGN KEY (OrganizationId, CustomerId)
            REFERENCES crm.Customers (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes') AND name = N'FK_CustomerNotes_CreatedBy_Tenant')
    BEGIN
        ALTER TABLE crm.CustomerNotes WITH CHECK
        ADD CONSTRAINT FK_CustomerNotes_CreatedBy_Tenant
            FOREIGN KEY (OrganizationId, CreatedBy)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes') AND name = N'FK_CustomerNotes_DeletedBy_Tenant')
    BEGIN
        ALTER TABLE crm.CustomerNotes WITH CHECK
        ADD CONSTRAINT FK_CustomerNotes_DeletedBy_Tenant
            FOREIGN KEY (OrganizationId, DeletedBy)
            REFERENCES [identity].Users (OrganizationId, Id);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'crm.CustomerNotes')
          AND name = N'CK_CustomerNotes_SoftDelete'
    )
    BEGIN
        ALTER TABLE crm.CustomerNotes WITH CHECK
        ADD CONSTRAINT CK_CustomerNotes_SoftDelete
            CHECK
            (
                (IsDeleted = 0 AND DeletedAt IS NULL)
                OR
                (IsDeleted = 1 AND DeletedAt IS NOT NULL)
            );
    END;

    ---------------------------------------------------------------------------
    -- 9. Sign-in and audit-log enhancements
    ---------------------------------------------------------------------------
    IF COL_LENGTH(N'identity.SignInLogs', N'NormalizedEmailAttempted') IS NULL
        ALTER TABLE [identity].SignInLogs
            ADD NormalizedEmailAttempted AS (UPPER(LTRIM(RTRIM(EmailAttempted)))) PERSISTED;

    IF COL_LENGTH(N'identity.SignInLogs', N'EventType') IS NULL
        ALTER TABLE [identity].SignInLogs ADD EventType NVARCHAR(50) NOT NULL
            CONSTRAINT DF_SignInLogs_EventType DEFAULT N'PasswordSignIn';

    IF COL_LENGTH(N'identity.SignInLogs', N'AuthenticationMethod') IS NULL
        ALTER TABLE [identity].SignInLogs ADD AuthenticationMethod NVARCHAR(50) NULL;

    IF COL_LENGTH(N'identity.SignInLogs', N'DeviceId') IS NULL
        ALTER TABLE [identity].SignInLogs ADD DeviceId NVARCHAR(200) NULL;

    IF COL_LENGTH(N'identity.SignInLogs', N'CorrelationId') IS NULL
        ALTER TABLE [identity].SignInLogs ADD CorrelationId UNIQUEIDENTIFIER NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].SignInLogs') AND name = N'IX_SignInLogs_OrganizationId_CreatedAt')
    BEGIN
        CREATE INDEX IX_SignInLogs_OrganizationId_CreatedAt
            ON [identity].SignInLogs (OrganizationId, CreatedAt DESC)
            INCLUDE (UserId, IsSuccessful, EventType, IpAddress);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'[identity].SignInLogs') AND name = N'IX_SignInLogs_NormalizedEmailAttempted_CreatedAt')
    BEGIN
        CREATE INDEX IX_SignInLogs_NormalizedEmailAttempted_CreatedAt
            ON [identity].SignInLogs (NormalizedEmailAttempted, CreatedAt DESC)
            INCLUDE (OrganizationId, UserId, IsSuccessful, FailureReason);
    END;

    IF COL_LENGTH(N'audit.AuditLogs', N'RequestId') IS NULL
        ALTER TABLE audit.AuditLogs ADD RequestId NVARCHAR(100) NULL;

    IF COL_LENGTH(N'audit.AuditLogs', N'CorrelationId') IS NULL
        ALTER TABLE audit.AuditLogs ADD CorrelationId UNIQUEIDENTIFIER NULL;

    IF COL_LENGTH(N'audit.AuditLogs', N'Source') IS NULL
        ALTER TABLE audit.AuditLogs ADD [Source] NVARCHAR(100) NULL;

    IF COL_LENGTH(N'audit.AuditLogs', N'UserAgent') IS NULL
        ALTER TABLE audit.AuditLogs ADD UserAgent NVARCHAR(500) NULL;

    IF COL_LENGTH(N'audit.AuditLogs', N'Succeeded') IS NULL
        ALTER TABLE audit.AuditLogs ADD Succeeded BIT NOT NULL
            CONSTRAINT DF_AuditLogs_Succeeded DEFAULT 1;

    IF COL_LENGTH(N'audit.AuditLogs', N'FailureReason') IS NULL
        ALTER TABLE audit.AuditLogs ADD FailureReason NVARCHAR(500) NULL;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'audit.AuditLogs')
          AND name = N'CK_AuditLogs_OldValuesJson'
    )
    BEGIN
        ALTER TABLE audit.AuditLogs WITH CHECK
        ADD CONSTRAINT CK_AuditLogs_OldValuesJson
            CHECK (OldValuesJson IS NULL OR ISJSON(OldValuesJson) = 1);
    END;

    IF NOT EXISTS
    (
        SELECT 1 FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'audit.AuditLogs')
          AND name = N'CK_AuditLogs_NewValuesJson'
    )
    BEGIN
        ALTER TABLE audit.AuditLogs WITH CHECK
        ADD CONSTRAINT CK_AuditLogs_NewValuesJson
            CHECK (NewValuesJson IS NULL OR ISJSON(NewValuesJson) = 1);
    END;

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'audit.AuditLogs') AND name = N'IX_AuditLogs_CorrelationId')
    BEGIN
        CREATE INDEX IX_AuditLogs_CorrelationId
            ON audit.AuditLogs (CorrelationId)
            WHERE CorrelationId IS NOT NULL;
    END;

    ---------------------------------------------------------------------------
    -- 10. Migration metadata
    ---------------------------------------------------------------------------
    INSERT INTO dbo.SchemaVersions (MigrationId, [Name], [Checksum])
    VALUES
    (
        N'009',
        N'Harden Multi-Tenant Identity and Core Foundation',
        NULL
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
