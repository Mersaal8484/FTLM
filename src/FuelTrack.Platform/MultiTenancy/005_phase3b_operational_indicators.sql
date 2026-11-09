-- Phase 3b completion: additive station policies for operational indicators.
-- This script is safe for existing PostgreSQL tenant schemas.

ALTER TABLE IF EXISTS "StationOperationalSettings"
    ADD COLUMN IF NOT EXISTS "MissingDataIntervalMinutes" integer NOT NULL DEFAULT 60,
    ADD COLUMN IF NOT EXISTS "AverageLoadWarningTolerancePercent" numeric(5,2) NOT NULL DEFAULT 15.0,
    ADD COLUMN IF NOT EXISTS "FuelReconciliationWarningThresholdPercent" numeric(5,2) NOT NULL DEFAULT 10.0;
