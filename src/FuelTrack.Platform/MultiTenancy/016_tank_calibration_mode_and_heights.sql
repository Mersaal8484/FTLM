-- Tank calibration modes (Geometry = 0, LitresPerCm = 1) and height-entered capacity/threshold fields.
-- Matches EF Core migration 20261125120000_AddTankCalibrationModeAndHeights.
-- Idempotent PostgreSQL tenant upgrade; defaults keep existing rows valid.

ALTER TABLE "FuelTanks"
    ADD COLUMN IF NOT EXISTS "CalibrationMode" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "LitresPerCm" numeric(18,6),
    ADD COLUMN IF NOT EXISTS "CapacityHeightCm" numeric(18,4),
    ADD COLUMN IF NOT EXISTS "MinAlertHeightCm" numeric(18,4),
    ADD COLUMN IF NOT EXISTS "ActualStockHeightCm" numeric(18,4),
    ADD COLUMN IF NOT EXISTS "CapacityInputUnit" varchar(16) NOT NULL DEFAULT 'litres',
    ADD COLUMN IF NOT EXISTS "MinAlertInputUnit" varchar(16) NOT NULL DEFAULT 'litres',
    ADD COLUMN IF NOT EXISTS "ActualStockInputUnit" varchar(16) NOT NULL DEFAULT 'litres';

ALTER TABLE "TankLevelReadings"
    ADD COLUMN IF NOT EXISTS "CalibrationModeSnapshot" integer NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS "LitresPerCmSnapshot" numeric(18,6);
