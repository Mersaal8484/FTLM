-- ==============================================================================
-- PRE-DEPLOYMENT CHECK: Fuel Tanks with Shape = 1 (formerly HorizontalCylinderWithConicalEnds)
-- ==============================================================================
-- Purpose:
--   Run this script BEFORE deploying to production to confirm that no FuelTanks
--   rows with Shape = 1 exist. The HorizontalCylinderWithConicalEnds geometry has
--   been removed from the application entirely (Job 2, branch development).
--   Shape = 1 is now an undefined enum value rejected at the API boundary.
--
-- If this query returns rows:
--   The owner must decide how to handle them manually. Horizontal dimensions
--   (diameter, cylinder length, end-cone depth) have different physical meanings
--   from vertical dimensions and MUST NOT be automatically converted.
--   Contact the station administrator before deploying.
--
-- Safety:
--   READ-ONLY. This script DOES NOT modify, update, or convert any records.
--   Horizontal dimensions (diameter, cylinder length, end-cone depth) have
--   different physical meanings from vertical dimensions (diameter, cylinder
--   height, bottom-cone height) and must NOT be automatically converted.
--   If rows exist, the station administrator decides how to recalibrate them.
-- ==============================================================================

SELECT
    t.Id,
    t.Code,
    t.Name,
    t.TankRole,
    CASE t.TankRole
        WHEN 1 THEN 'Main'
        WHEN 2 THEN 'Daily'
        ELSE 'Unspecified'
    END AS TankRoleName,
    t.Shape,
    CASE t.Shape
        WHEN 0 THEN 'VerticalCylinderWithConicalBottom'
        WHEN 1 THEN 'HorizontalCylinderWithConicalEnds (Obsolete)'
        ELSE 'Unknown'
    END AS ShapeName,
    t.DiameterCm,
    t.CylinderHeightCm,
    t.ConeHeightCm,
    t.MultiplicationFactor,
    t.CapacityLiters,
    t.ComputedCapacityLitres,
    t.DeadStockLiters,
    t.DeadStockLevelCm,
    t.DeadStockInputUnit,
    t.ActualStockLiters,
    t.SubStationId,
    s.Name AS SubStationName,
    t.GeneratorId,
    g.Name AS GeneratorName,
    t.ParentTankId,
    t.IsActive,
    t.CreatedAt,
    t.UpdatedAt
FROM FuelTanks t
LEFT JOIN SubStations s ON t.SubStationId = s.Id
LEFT JOIN Generators g ON t.GeneratorId = g.Id
WHERE t.Shape = 1
ORDER BY t.Id;
