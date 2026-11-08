-- Phase 3: Review, approval and derived-data columns
-- Additive only; safe to run more than once.

-- OperationalSubmission: track the original submitter for approver ≠ submitter rule (D18)
ALTER TABLE "OperationalSubmissions"
    ADD COLUMN IF NOT EXISTS "SubmittedByUserId" integer;

-- OperationalGeneratorReadings: approval / rejection audit
ALTER TABLE "OperationalGeneratorReadings"
    ADD COLUMN IF NOT EXISTS "ApprovedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "ApprovedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "RejectedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectionReason"  text;

-- TankLevelReadings: approval / rejection audit
ALTER TABLE "TankLevelReadings"
    ADD COLUMN IF NOT EXISTS "ApprovedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "ApprovedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "RejectedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectionReason"  text;

-- FeederLoadReadings: approval / rejection audit
ALTER TABLE "FeederLoadReadings"
    ADD COLUMN IF NOT EXISTS "ApprovedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "ApprovedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "RejectedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectionReason"  text;

-- MeterReadings: approval / rejection audit
ALTER TABLE "MeterReadings"
    ADD COLUMN IF NOT EXISTS "ApprovedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "ApprovedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectedAt"       timestamp with time zone,
    ADD COLUMN IF NOT EXISTS "RejectedByUserId" integer,
    ADD COLUMN IF NOT EXISTS "RejectionReason"  text;

-- StationOperationalSettings: optional internal-consumption meter for energy balance (§7.5)
ALTER TABLE "StationOperationalSettings"
    ADD COLUMN IF NOT EXISTS "InternalConsumptionMeterId" integer;

-- Index: speed up pending-submission review queue
CREATE INDEX IF NOT EXISTS "IX_OperationalSubmissions_Status"
    ON "OperationalSubmissions" ("Status")
    WHERE "DeletedAt" IS NULL;

-- Index: speed up daily-closing delta queries
CREATE INDEX IF NOT EXISTS "IX_MeterReadings_Subject_Kind_Status"
    ON "MeterReadings" ("SubjectType", "SubjectId", "Kind", "Status")
    WHERE "DeletedAt" IS NULL;
