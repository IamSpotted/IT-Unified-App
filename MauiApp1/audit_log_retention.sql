-- Audit Log Retention Management
-- Implements 1-year retention policy for audit logs (similar to deleted_devices)

USE it_device_inventory;
GO

-- Create stored procedure for audit log cleanup (1-year retention)
CREATE OR ALTER PROCEDURE CleanupOldAuditLogs
    @RetentionDays INT = 365,  -- Default 1 year retention
    @DryRun BIT = 1           -- Default to dry run (preview only)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -@RetentionDays, GETDATE());
    DECLARE @RecordsToDelete INT;
    
    -- Count records that would be deleted
    SELECT @RecordsToDelete = COUNT(*)
    FROM device_audit_log 
    WHERE performed_at < @CutoffDate;
    
    PRINT 'Audit Log Cleanup Analysis:';
    PRINT '- Cutoff Date: ' + CONVERT(NVARCHAR(20), @CutoffDate, 120);
    PRINT '- Records older than ' + CAST(@RetentionDays AS NVARCHAR(10)) + ' days: ' + CAST(@RecordsToDelete AS NVARCHAR(10));
    
    IF @RecordsToDelete = 0
    BEGIN
        PRINT '- No old audit records to clean up.';
        RETURN;
    END
    
    IF @DryRun = 1
    BEGIN
        PRINT '- DRY RUN MODE: No records will be deleted.';
        PRINT '- To actually delete these records, run: EXEC CleanupOldAuditLogs @RetentionDays = ' + CAST(@RetentionDays AS NVARCHAR(10)) + ', @DryRun = 0';
        
        -- Show sample of records that would be deleted
        PRINT '';
        PRINT 'Sample of records that would be deleted:';
        SELECT TOP 10 
            log_id,
            device_id,
            action_type,
            performed_at,
            performed_by,
            DATEDIFF(DAY, performed_at, GETDATE()) AS days_old
        FROM device_audit_log 
        WHERE performed_at < @CutoffDate
        ORDER BY performed_at;
    END
    ELSE
    BEGIN
        -- Actually delete the old records
        DELETE FROM device_audit_log 
        WHERE performed_at < @CutoffDate;
        
        PRINT '- DELETED ' + CAST(@@ROWCOUNT AS NVARCHAR(10)) + ' old audit log records.';
        PRINT '- Audit log cleanup completed successfully.';
    END
END;
GO

-- Create a scheduled job helper (you can set this up in SQL Server Agent)
PRINT '';
PRINT 'To set up automatic cleanup, create a SQL Server Agent job that runs:';
PRINT 'EXEC CleanupOldAuditLogs @RetentionDays = 365, @DryRun = 0';
PRINT '';
PRINT 'For immediate testing, run:';
PRINT 'EXEC CleanupOldAuditLogs @RetentionDays = 365, @DryRun = 1  -- Preview only';
GO
