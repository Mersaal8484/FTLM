-- Phase 3b.1 corrections: preserve the original dead-stock input unit.
-- This is additive and must run after upgrade 006; existing values remain litres.

ALTER TABLE IF EXISTS "FuelTanks"
    ADD COLUMN IF NOT EXISTS "DeadStockLevelCm" numeric(18,4);

ALTER TABLE IF EXISTS "FuelTanks"
    ADD COLUMN IF NOT EXISTS "DeadStockInputUnit" varchar(16) NOT NULL DEFAULT 'litres';
