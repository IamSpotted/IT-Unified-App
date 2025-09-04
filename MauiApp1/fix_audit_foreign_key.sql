-- Fix for audit log foreign key constraint that prevents deletion
-- Since audit logs should be immutable, we should remove the foreign key constraint

USE device_inventory;
GO

-- Drop the foreign key constraint from device_audit_log
-- This allows audit logs to persist even after devices are deleted
IF EXISTS (
    SELECT * FROM sys.foreign_keys 
    WHERE name = 'FK_device_audit_log_device_id'
)
BEGIN
    ALTER TABLE device_audit_log DROP CONSTRAINT FK_device_audit_log_device_id;
    PRINT 'Removed foreign key constraint FK_device_audit_log_device_id from device_audit_log table';
END
ELSE
BEGIN
    PRINT 'Foreign key constraint FK_device_audit_log_device_id not found - may already be removed';
END

-- Verify the constraint was removed
SELECT 
    fk.name AS constraint_name,
    t.name AS table_name,
    c.name AS column_name
FROM sys.foreign_keys fk
JOIN sys.tables t ON fk.parent_object_id = t.object_id
JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
JOIN sys.columns c ON fkc.parent_object_id = c.object_id AND fkc.parent_column_id = c.column_id
WHERE t.name = 'device_audit_log';

PRINT 'Foreign key constraints verification complete. If no rows are shown above, all constraints have been removed from device_audit_log.';
