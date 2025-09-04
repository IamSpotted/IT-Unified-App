-- Fix for audit log CASCADE DELETE issue
-- Problem: device_audit_log has ON DELETE CASCADE which removes audit entries when devices are deleted
-- Solution: Remove CASCADE DELETE to make audit logs immutable until they age out naturally

USE it_device_inventory;
GO

-- Step 1: Find the current foreign key constraint
PRINT 'Current foreign key constraints on device_audit_log:';
SELECT 
    fk.name AS constraint_name,
    fk.delete_referential_action_desc AS delete_action,
    'ALTER TABLE device_audit_log DROP CONSTRAINT ' + fk.name + ';' AS drop_statement
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('device_audit_log')
  AND fk.referenced_object_id = OBJECT_ID('devices');
GO

-- Step 2: Drop the CASCADE DELETE constraint
-- Find the constraint name from Step 1 and replace [CONSTRAINT_NAME] below
DECLARE @sql NVARCHAR(MAX);
SELECT @sql = 'ALTER TABLE device_audit_log DROP CONSTRAINT ' + fk.name + ';'
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('device_audit_log')
  AND fk.referenced_object_id = OBJECT_ID('devices');

IF @sql IS NOT NULL
BEGIN
    PRINT 'Dropping existing foreign key constraint...';
    EXEC sp_executesql @sql;
    PRINT 'Constraint dropped successfully.';
END
ELSE
BEGIN
    PRINT 'No foreign key constraint found.';
END
GO

-- Step 3: Add foreign key WITHOUT CASCADE DELETE
-- This preserves audit entries even after devices are deleted
ALTER TABLE device_audit_log 
ADD CONSTRAINT FK_device_audit_log_device_id 
FOREIGN KEY (device_id) REFERENCES devices(device_id);
GO

PRINT 'Audit log foreign key recreated WITHOUT CASCADE DELETE.';
PRINT 'Audit logs will now be preserved when devices are deleted.';
GO

-- Step 4: Verify the fix
SELECT 
    fk.name AS constraint_name,
    fk.delete_referential_action_desc AS delete_action,
    CASE 
        WHEN fk.delete_referential_action_desc = 'NO_ACTION' THEN 'CORRECT - Audit logs will be preserved'
        WHEN fk.delete_referential_action_desc = 'CASCADE' THEN 'PROBLEM - Audit logs will still be deleted'
        ELSE 'UNKNOWN - ' + fk.delete_referential_action_desc
    END AS status
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('device_audit_log')
  AND fk.referenced_object_id = OBJECT_ID('devices');
GO
