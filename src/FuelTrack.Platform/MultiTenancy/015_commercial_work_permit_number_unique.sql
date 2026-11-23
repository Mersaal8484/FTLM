-- FuelTrack Multi-Tenant Schema Upgrade: Version 15
-- WorkPermit PermitNumber unique index (commercial and enterprise)
ALTER TABLE "WorkPermits" ALTER COLUMN "PermitNumber" TYPE character varying(100);
CREATE UNIQUE INDEX IF NOT EXISTS "UX_WorkPermits_PermitNumber" ON "WorkPermits" ("PermitNumber") WHERE "DeletedAt" IS NULL;
