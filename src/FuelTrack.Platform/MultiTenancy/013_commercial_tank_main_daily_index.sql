-- Commercial tanks: allow daily tanks for multiple generators while retaining one active main tank per fuel type.
-- Idempotent PostgreSQL tenant upgrade.

DROP INDEX IF EXISTS "IX_FuelTanks_SubStationId_FuelLookupId";

CREATE UNIQUE INDEX IF NOT EXISTS "IX_FuelTanks_SubStationId_FuelLookupId"
    ON "FuelTanks" ("SubStationId", "FuelLookupId")
    WHERE "DeletedAt" IS NULL AND "GeneratorId" IS NULL;
