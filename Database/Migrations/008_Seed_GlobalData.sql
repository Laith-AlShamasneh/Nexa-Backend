-- Migration 008: Seed global (platform-wide) data
-- Global = not tied to any OrganizationId: the Permission catalog and the four
-- system Role templates. Idempotent (safe to re-run on every deploy).

MERGE [identity].Permissions AS target
USING (VALUES
    ('Organization.View',   'View organization',      'Organization'),
    ('Organization.Manage', 'Manage organization',    'Organization'),
    ('Branch.View',         'View branches',          'Branch'),
    ('Branch.Manage',       'Manage branches',        'Branch'),
    ('User.View',           'View users',             'User'),
    ('User.Manage',         'Manage users',           'User'),
    ('Role.Manage',         'Manage roles',            'Role'),
    ('Customer.View',       'View customers',         'Customer'),
    ('Customer.Create',     'Create customers',       'Customer'),
    ('Customer.Update',     'Update customers',       'Customer'),
    ('Customer.Delete',     'Delete customers',       'Customer'),
    ('Payment.View',        'View payments',          'Payment'),
    ('Payment.Create',      'Record payments',        'Payment'),
    ('Report.View',         'View reports',           'Report'),
    ('Attendance.View',     'View attendance',        'Attendance'),
    ('Attendance.Record',   'Record attendance',      'Attendance')
) AS source (Code, Name, Module)
ON target.Code = source.Code
WHEN NOT MATCHED THEN
    INSERT (Code, Name, Module) VALUES (source.Code, source.Name, source.Module);
GO

MERGE [identity].Roles AS target
USING (VALUES
    ('Owner',      'Full access to the organization', 1),
    ('Admin',      'Administrative access',            1),
    ('Accountant', 'Billing and payments access',      1),
    ('Teacher',    'Class and attendance access',      1)
) AS source (Name, Description, IsSystemRole)
ON target.Name = source.Name AND target.OrganizationId IS NULL
WHEN NOT MATCHED THEN
    INSERT (OrganizationId, Name, Description, IsSystemRole)
    VALUES (NULL, source.Name, source.Description, source.IsSystemRole);
GO

-- Owner: every permission in the catalog.
INSERT INTO [identity].RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM [identity].Roles r
CROSS JOIN [identity].Permissions p
WHERE r.Name = 'Owner' AND r.OrganizationId IS NULL
  AND NOT EXISTS (
      SELECT 1 FROM [identity].RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- Accountant: payments + read-only customer/report access.
INSERT INTO [identity].RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM [identity].Roles r
CROSS JOIN [identity].Permissions p
WHERE r.Name = 'Accountant' AND r.OrganizationId IS NULL
  AND p.Code IN ('Customer.View', 'Payment.View', 'Payment.Create', 'Report.View')
  AND NOT EXISTS (
      SELECT 1 FROM [identity].RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO

-- Teacher: attendance + read-only customer access.
INSERT INTO [identity].RolePermissions (RoleId, PermissionId)
SELECT r.Id, p.Id
FROM [identity].Roles r
CROSS JOIN [identity].Permissions p
WHERE r.Name = 'Teacher' AND r.OrganizationId IS NULL
  AND p.Code IN ('Customer.View', 'Attendance.View', 'Attendance.Record')
  AND NOT EXISTS (
      SELECT 1 FROM [identity].RolePermissions rp WHERE rp.RoleId = r.Id AND rp.PermissionId = p.Id
  );
GO
