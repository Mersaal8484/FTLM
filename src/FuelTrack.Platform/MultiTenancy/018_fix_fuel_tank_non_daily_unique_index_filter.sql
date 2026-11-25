-- Scope IX_FuelTanks_SubStationId_FuelLookupId to active non-daily tanks (TankRole IS NULL OR TankRole <> 2).
-- Idempotent PostgreSQL tenant upgrade.

DROP INDEX IF EXISTS "IX_FuelTanks_SubStationId_FuelLookupId";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FuelTanks_SubStationId_FuelLookupId"
    ON "FuelTanks" ("SubStationId", "FuelLookupId")
    WHERE "DeletedAt" IS NULL AND ("TankRole" IS NULL OR "TankRole" <> 2);
