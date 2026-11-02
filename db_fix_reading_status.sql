-- Fix script for reading approval in "قراءات الطاقة المرسلة" tab
-- Run this after starting the SQL Server service (MSSQLSERVER)

-- 1. Add missing columns to OilTransactions (if they don't exist)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'CorrectedFromId')
    ALTER TABLE OilTransactions ADD CorrectedFromId INT NULL;

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OilTransactions') AND name = 'CorrectionReason')
    ALTER TABLE OilTransactions ADD CorrectionReason NVARCHAR(MAX) NULL;

-- 2. Insert ReadingStatus lookup category (if it doesn't exist)
IF NOT EXISTS (SELECT * FROM LookupCategories WHERE Name = 'ReadingStatus')
    INSERT INTO LookupCategories (Id, Name, DisplayName, Description, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (19, 'ReadingStatus', 'حالة الموقع', 'Reading status: Draft, Pending, Submitted, Approved, Rejected', 1, '2026-01-01', 0, '2026-01-01', 0);

-- 3. Insert ReadingStatus lookups (if they don't exist)
IF NOT EXISTS (SELECT * FROM Lookups WHERE CategoryId IN (SELECT Id FROM LookupCategories WHERE Name = 'ReadingStatus') AND Code = 'Draft')
    INSERT INTO Lookups (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (55, 19, 'Draft', 'مسودة', 1, 1, '2026-01-01', 0, '2026-01-01', 0);

IF NOT EXISTS (SELECT * FROM Lookups WHERE CategoryId IN (SELECT Id FROM LookupCategories WHERE Name = 'ReadingStatus') AND Code = 'Pending')
    INSERT INTO Lookups (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (56, 19, 'Pending', 'معلق', 2, 1, '2026-01-01', 0, '2026-01-01', 0);

IF NOT EXISTS (SELECT * FROM Lookups WHERE CategoryId IN (SELECT Id FROM LookupCategories WHERE Name = 'ReadingStatus') AND Code = 'Submitted')
    INSERT INTO Lookups (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (57, 19, 'Submitted', 'مرسل', 3, 1, '2026-01-01', 0, '2026-01-01', 0);

IF NOT EXISTS (SELECT * FROM Lookups WHERE CategoryId IN (SELECT Id FROM LookupCategories WHERE Name = 'ReadingStatus') AND Code = 'Approved')
    INSERT INTO Lookups (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (58, 19, 'Approved', 'معتمد', 4, 1, '2026-01-01', 0, '2026-01-01', 0);

IF NOT EXISTS (SELECT * FROM Lookups WHERE CategoryId IN (SELECT Id FROM LookupCategories WHERE Name = 'ReadingStatus') AND Code = 'Rejected')
    INSERT INTO Lookups (Id, CategoryId, Code, Name, SortOrder, IsActive, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
    VALUES (59, 19, 'Rejected', 'مرفوض', 5, 1, '2026-01-01', 0, '2026-01-01', 0);

-- 4. Add FK constraint for SubStationReadings.ReadingStatusLookupId (if it doesn't exist)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_SubStationReadings_Lookups_ReadingStatusLookupId')
    ALTER TABLE SubStationReadings ADD CONSTRAINT FK_SubStationReadings_Lookups_ReadingStatusLookupId
    FOREIGN KEY (ReadingStatusLookupId) REFERENCES Lookups(Id);

PRINT 'ReadingStatus lookups inserted and schema fixed successfully.';
