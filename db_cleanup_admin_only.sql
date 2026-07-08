-- ============================================================
-- Clean all data EXCEPT admin user (admin@ft.com / Admin@123)
-- Keeps: reference tables (Branches, Stations, SubStations,
--         Lookups, LookupCategories), admin user + role
-- ============================================================
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Hangfire tables (safe to clear completely)
    DELETE FROM [Hangfire].[State];
    DELETE FROM [Hangfire].[JobParameter];
    DELETE FROM [Hangfire].[JobQueue];
    DELETE FROM [Hangfire].[Job];
    DELETE FROM [Hangfire].[Hash];
    DELETE FROM [Hangfire].[List];
    DELETE FROM [Hangfire].[Set];
    DELETE FROM [Hangfire].[Counter];
    DELETE FROM [Hangfire].[AggregatedCounter];
    DELETE FROM [Hangfire].[Server];

    -- 2. User-related data (keep only admin Id=1)
    DELETE FROM UserRoles WHERE UserId != 1;
    DELETE FROM UserRoles WHERE UserId = 1 AND RoleId != 1;  -- keep only admin role
    DELETE FROM UserScopes WHERE UserId != 1;
    DELETE FROM RefreshTokens WHERE UserId != 1;
    DELETE FROM LoginAttempts WHERE UserId != 1;
    DELETE FROM UserClaims WHERE UserId != 1;
    DELETE FROM UserLogins WHERE UserId != 1;
    DELETE FROM UserTokens WHERE UserId != 1;

    -- 3. Delete non-admin users
    DELETE FROM Users WHERE Id != 1;

    -- 4. Business/transactional data
    DELETE FROM AuditLogs;
    DELETE FROM NotificationRecipients;
    DELETE FROM Notifications;
    DELETE FROM InsightFeedbacks;
    DELETE FROM AnalyticsInsights;
    DELETE FROM SubStationReadings;
    DELETE FROM DailyReadings;
    DELETE FROM FuelTransactions;
    DELETE FROM OilTransactions;
    DELETE FROM PeriodicInventoryFuels;
    DELETE FROM PeriodicInventoryOils;
    DELETE FROM PeriodicInventories;
    DELETE FROM GeneratorFuels;
    DELETE FROM GeneratorOils;
    DELETE FROM FuelTanks;
    DELETE FROM Oils;
    DELETE FROM Meters;
    DELETE FROM Controllers;
    DELETE FROM Generators;

    -- 5. Reference structure (clear but keep schema)
    DELETE FROM SubStations;
    DELETE FROM Stations;
    DELETE FROM Branches;

    -- 6. Keep Lookups & LookupCategories (app needs them)
    -- No delete here

    -- 7. Reset identity seeds for key tables
    DBCC CHECKIDENT ('Users', RESEED, 1);
    DBCC CHECKIDENT ('Branches', RESEED, 0);
    DBCC CHECKIDENT ('Stations', RESEED, 0);
    DBCC CHECKIDENT ('SubStations', RESEED, 0);
    DBCC CHECKIDENT ('Generators', RESEED, 0);
    DBCC CHECKIDENT ('FuelTanks', RESEED, 0);
    DBCC CHECKIDENT ('Oils', RESEED, 0);
    DBCC CHECKIDENT ('Meters', RESEED, 0);
    DBCC CHECKIDENT ('Controllers', RESEED, 0);
    DBCC CHECKIDENT ('DailyReadings', RESEED, 0);
    DBCC CHECKIDENT ('FuelTransactions', RESEED, 0);
    DBCC CHECKIDENT ('OilTransactions', RESEED, 0);
    DBCC CHECKIDENT ('PeriodicInventories', RESEED, 0);
    DBCC CHECKIDENT ('AnalyticsInsights', RESEED, 0);
    DBCC CHECKIDENT ('AuditLogs', RESEED, 0);
    DBCC CHECKIDENT ('Notifications', RESEED, 0);

    COMMIT TRANSACTION;
    PRINT 'SUCCESS: All data cleared. Admin user preserved.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
END CATCH
