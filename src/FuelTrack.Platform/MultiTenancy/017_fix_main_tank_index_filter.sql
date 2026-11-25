-- Fix IX_FuelTanks_SubStationId_FuelLookupId to scope to Main tanks only (TankRole = 1).
-- Idempotent PostgreSQL tenant upgrade.

DROP INDEX IF EXISTS "IX_FuelTanks_SubStationId_FuelLookupId";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FuelTanks_SubStationId_FuelLookupId"
    ON "FuelTanks" ("SubStationId", "FuelLookupId")
    WHERE "TankRole" = 1 AND "DeletedAt" IS NULL;
