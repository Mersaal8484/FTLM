-- Phase 4 corrective upgrade: repair only the original mojibake seed values
-- emitted by upgrade 008. Do not overwrite tenant-edited service item names.

UPDATE "ServiceItems"
SET "NameAr" = U&'\0628\062F\0644\0629 \0632\064A\062A',
    "UpdatedAt" = NOW()
WHERE "Code" = 'OilChangeSet'
  AND "NameAr" = convert_from(convert_to(U&'\0628\062F\0644\0629 \0632\064A\062A', 'UTF8'), 'LATIN1');

UPDATE "ServiceItems"
SET "NameAr" = U&'\0641\0644\062A\0631 \0627\0644\0632\064A\062A',
    "UpdatedAt" = NOW()
WHERE "Code" = 'OilFilter'
  AND "NameAr" = convert_from(convert_to(U&'\0641\0644\062A\0631 \0627\0644\0632\064A\062A', 'UTF8'), 'LATIN1');

UPDATE "ServiceItems"
SET "NameAr" = U&'\0641\0644\062A\0631 \0627\0644\0647\0648\0627\0621',
    "UpdatedAt" = NOW()
WHERE "Code" = 'AirFilter'
  AND "NameAr" = convert_from(convert_to(U&'\0641\0644\062A\0631 \0627\0644\0647\0648\0627\0621', 'UTF8'), 'LATIN1');

UPDATE "ServiceItems"
SET "NameAr" = U&'\0627\0644\0646\0648\0632\0644\0627\062A',
    "UpdatedAt" = NOW()
WHERE "Code" = 'Nozzles'
  AND "NameAr" = convert_from(convert_to(U&'\0627\0644\0646\0648\0632\0644\0627\062A', 'UTF8'), 'LATIN1');

UPDATE "ServiceItems"
SET "NameAr" = U&'\0641\0644\062A\0631 \0627\0644\0648\0642\0648\062F',
    "UpdatedAt" = NOW()
WHERE "Code" = 'FuelFilter'
  AND "NameAr" = convert_from(convert_to(U&'\0641\0644\062A\0631 \0627\0644\0648\0642\0648\062F', 'UTF8'), 'LATIN1');

UPDATE "ServiceItems"
SET "NameAr" = U&'\0636\0628\0637 \0627\0644\0635\0645\0627\0645\0627\062A',
    "UpdatedAt" = NOW()
WHERE "Code" = 'ValveAdjustment'
  AND "NameAr" = convert_from(convert_to(U&'\0636\0628\0637 \0627\0644\0635\0645\0627\0645\0627\062A', 'UTF8'), 'LATIN1');
