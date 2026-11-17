-- Phase C3 / C3.2: Add FuelTanks.TankRole for commercial station mode
-- Matches EF Core migration 20261116120000_AddTankRoleAndHorizontalShape
-- Idempotent, additive, transactional.
--
-- Semantics:
--   NULL = enterprise (no role constraint)
--   1    = Main tank
--   2    = Daily tank
--
-- Rollback plan:
--   DROP INDEX IF EXISTS "IX_FuelTanks_TankRole";
--   ALTER TABLE "FuelTanks" DROP COLUMN IF EXISTS "TankRole";

ALTER TABLE "FuelTanks"
    ADD COLUMN IF NOT EXISTS "TankRole" integer;

COMMENT ON COLUMN "FuelTanks"."TankRole" IS
    'Commercial station tank role: NULL=enterprise, 1=Main, 2=Daily. Added C3/C3.2.';

-- Index for filtering tanks by role (e.g. daily tanks linked to generators)
CREATE INDEX IF NOT EXISTS "IX_FuelTanks_TankRole"
    ON "FuelTanks" ("TankRole")
    WHERE "TankRole" IS NOT NULL AND "DeletedAt" IS NULL;
