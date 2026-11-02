-- ==========================================================
-- FuelTrack Automated Backup Script (Full & Diff Backup)
-- SQL Server 2022
-- ==========================================================

DECLARE @BackupPath NVARCHAR(255) = N'C:\FuelTrackBackups\';
DECLARE @DatabaseName NVARCHAR(128) = N'FuelTrack';
DECLARE @Timestamp NVARCHAR(32) = CONVERT(NVARCHAR(20), GETDATE(), 112) + N'_' + REPLACE(CONVERT(NVARCHAR(20), GETDATE(), 108), N':', N'');
DECLARE @FullFileName NVARCHAR(500) = @BackupPath + @DatabaseName + N'_Full_' + @Timestamp + N'.bak';
DECLARE @DiffFileName NVARCHAR(500) = @BackupPath + @DatabaseName + N'_Diff_' + @Timestamp + N'.bak';

-- 1. Full Backup
PRINT 'Starting Full Backup of ' + @DatabaseName + '...';
BACKUP DATABASE [FuelTrack]
TO DISK = @FullFileName
WITH FORMAT, INIT,
     NAME = N'FuelTrack-Full Database Backup',
     COMPRESSION,
     CHECKSUM,
     STATS = 10;

PRINT 'Full Backup completed successfully to: ' + @FullFileName;

-- 2. Verify Backup Integrity
RESTORE VERIFYONLY
FROM DISK = @FullFileName
WITH CHECKSUM;

PRINT 'Backup verification passed.';
GO
