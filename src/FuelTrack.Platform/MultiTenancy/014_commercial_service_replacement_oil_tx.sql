-- Phase C6.2: Link service replacement records to consumed oil transactions.
-- Idempotent PostgreSQL tenant upgrade.

ALTER TABLE "ServiceReplacementRecords"
    ADD COLUMN IF NOT EXISTS "OilTransactionId" integer;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint WHERE conname = 'FK_ServiceReplacementRecords_OilTransactions_OilTransactionId'
    ) THEN
        ALTER TABLE "ServiceReplacementRecords"
            ADD CONSTRAINT "FK_ServiceReplacementRecords_OilTransactions_OilTransactionId"
            FOREIGN KEY ("OilTransactionId") REFERENCES "OilTransactions" ("Id") ON DELETE SET NULL;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS "IX_ServiceReplacementRecords_OilTransactionId"
    ON "ServiceReplacementRecords" ("OilTransactionId");
