/*
    Migration 010: Bilingual (English/Arabic) name fields

    Adds an Arabic counterpart to every user-facing "name" column across the
    schema, alongside the existing (English) column — no existing column is
    renamed or dropped:

        tenant.Organizations   Name, LegalName        -> + ArabicName, ArabicLegalName
        tenant.Branches        Name                   -> + ArabicName
        identity.Persons       FirstName, LastName,
                                FullName (computed)    -> + ArabicFirstName, ArabicLastName,
                                                           ArabicFullName (computed)
        identity.Roles         Name                   -> + ArabicName
        identity.Permissions   Name                   -> + ArabicName
        crm.Customers          DisplayName            -> + ArabicDisplayName

    All Arabic columns are nullable: not every existing row has an Arabic
    translation yet, and requiring one would break every row inserted by
    migrations 001-009. Every change is individually idempotent
    (COL_LENGTH guards), so this script is safe to re-run in full.

    Follows the same-batch column-visibility rule learned from migration 009:
    a GO separates adding ArabicFirstName/ArabicLastName from adding the
    ArabicFullName computed column that reads them, since a column added
    earlier in the same batch is not visible to a later statement in that
    batch.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'010')
BEGIN
    PRINT N'Migration 010 has already been applied; nothing to do.';
    RETURN;
END;
GO

BEGIN TRANSACTION Migration010;

---------------------------------------------------------------------------
-- tenant.Organizations
---------------------------------------------------------------------------
IF COL_LENGTH(N'tenant.Organizations', N'ArabicName') IS NULL
    ALTER TABLE tenant.Organizations ADD ArabicName NVARCHAR(200) NULL;

IF COL_LENGTH(N'tenant.Organizations', N'ArabicLegalName') IS NULL
    ALTER TABLE tenant.Organizations ADD ArabicLegalName NVARCHAR(200) NULL;

---------------------------------------------------------------------------
-- tenant.Branches
---------------------------------------------------------------------------
IF COL_LENGTH(N'tenant.Branches', N'ArabicName') IS NULL
    ALTER TABLE tenant.Branches ADD ArabicName NVARCHAR(200) NULL;

---------------------------------------------------------------------------
-- identity.Roles
---------------------------------------------------------------------------
IF COL_LENGTH(N'identity.Roles', N'ArabicName') IS NULL
    ALTER TABLE [identity].Roles ADD ArabicName NVARCHAR(100) NULL;

---------------------------------------------------------------------------
-- identity.Permissions
---------------------------------------------------------------------------
IF COL_LENGTH(N'identity.Permissions', N'ArabicName') IS NULL
    ALTER TABLE [identity].Permissions ADD ArabicName NVARCHAR(200) NULL;

---------------------------------------------------------------------------
-- crm.Customers
---------------------------------------------------------------------------
IF COL_LENGTH(N'crm.Customers', N'ArabicDisplayName') IS NULL
    ALTER TABLE crm.Customers ADD ArabicDisplayName NVARCHAR(200) NULL;

---------------------------------------------------------------------------
-- identity.Persons: ArabicFirstName / ArabicLastName, then the computed
-- ArabicFullName that reads them (needs its own batch — see header note).
---------------------------------------------------------------------------
IF COL_LENGTH(N'identity.Persons', N'ArabicFirstName') IS NULL
    ALTER TABLE [identity].Persons ADD ArabicFirstName NVARCHAR(100) NULL;

IF COL_LENGTH(N'identity.Persons', N'ArabicLastName') IS NULL
    ALTER TABLE [identity].Persons ADD ArabicLastName NVARCHAR(100) NULL;
GO

IF COL_LENGTH(N'identity.Persons', N'ArabicFullName') IS NULL
BEGIN
    ALTER TABLE [identity].Persons
        ADD ArabicFullName AS
        (
            CASE
                WHEN ArabicFirstName IS NULL AND ArabicLastName IS NULL THEN NULL
                ELSE LTRIM(RTRIM(CONCAT(ArabicFirstName, N' ', ArabicLastName)))
            END
        ) PERSISTED;
END;
GO

---------------------------------------------------------------------------
-- Migration metadata
---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE MigrationId = N'010')
BEGIN
    INSERT INTO dbo.SchemaVersions (MigrationId, [Name], [Checksum])
    VALUES (N'010', N'Bilingual (English/Arabic) Name Fields', NULL);
END;

COMMIT TRANSACTION Migration010;
