-- ============================================================
-- FuelTrack Full DB Cleanup
-- Keeps only: admin@ft.com (UserId=1), Admin Role (RoleId=1),
--             LookupCategories, Lookups (seed data)
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

PRINT '=== Phase 1: Junction / Leaf Tables ===';
DELETE FROM InsightFeedbacks;
DELETE FROM NotificationRecipients;
DELETE FROM GeneratorOils;
DELETE FROM GeneratorFuels;
DELETE FROM PeriodicInventoryFuels;
DELETE FROM PeriodicInventoryOils;
DELETE FROM DailyReadings;
DELETE FROM SubStationReadings;
DELETE FROM OilTransactions;
DELETE FROM FuelTransactions;
DELETE FROM UserScopes;

PRINT '=== Phase 2: Dependent Business Tables ===';
DELETE FROM PeriodicInventories;
DELETE FROM Oils;
DELETE FROM Controllers;
DELETE FROM Meters;
DELETE FROM FuelTanks;

PRINT '=== Phase 3: Generator -> SubStation -> Station -> Branch ===';
DELETE FROM Generators;
DELETE FROM SubStations;
DELETE FROM Stations;
DELETE FROM Branches;

PRINT '=== Phase 4: Support Tables ===';
DELETE FROM RefreshTokens;
DELETE FROM LoginAttempts;
DELETE FROM Notifications;
DELETE FROM AuditLogs;
DELETE FROM AnalyticsInsights;

PRINT '=== Phase 5: Hangfire Job Tables ===';
DELETE FROM Hangfire.AggregatedCounter;
DELETE FROM Hangfire.Counter;
DELETE FROM Hangfire.Hash;
DELETE FROM Hangfire.JobParameter;
DELETE FROM Hangfire.JobQueue;
DELETE FROM Hangfire.Job;
DELETE FROM Hangfire.List;
DELETE FROM Hangfire.[Schema];
DELETE FROM Hangfire.[Server];
DELETE FROM Hangfire.[Set];
DELETE FROM Hangfire.[State];

PRINT '=== Phase 6: Clean Identity Tables (Keep Admin Only) ===';
DELETE FROM UserRoles WHERE UserId != 1;
DELETE FROM RoleClaims WHERE RoleId != 1;
DELETE FROM UserClaims WHERE UserId != 1;
DELETE FROM UserTokens WHERE UserId != 1;
DELETE FROM UserLogins WHERE UserId != 1;
DELETE FROM Roles WHERE Id != 1;
DELETE FROM Users WHERE Id != 1;

PRINT '=== Phase 7: Verify ===';
SELECT 'REMAINING_USERS', Id, Email FROM Users;
SELECT 'REMAINING_ROLES', Id, Name FROM Roles;
SELECT 'REMAINING_USERROLES', UserId, RoleId FROM UserRoles;
SELECT 'REMAINING_BRANCHES', COUNT(*) AS Count FROM Branches;
SELECT 'REMAINING_LOOKUPS', COUNT(*) AS Count FROM Lookups;
SELECT 'REMAINING_LOOKUPCATEGORIES', COUNT(*) AS Count FROM LookupCategories;

COMMIT TRANSACTION;
PRINT '=== DONE: Cleanup Complete ===';
GO
