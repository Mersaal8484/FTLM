-- View: Current fuel balance per SubStation & FuelType
CREATE OR ALTER VIEW [dbo].[v_SubStationFuelStock] AS
SELECT 
    ftx.SubStationId,
    ftx.FuelLookupId,
    ISNULL(SUM(CASE 
        WHEN tl.Code IN ('Purchase', 'TransferIn', 'AdjustmentAdd') THEN ftx.QuantityLiters
        WHEN tl.Code IN ('Consumption', 'TransferOut', 'Loss', 'AdjustmentRemove') THEN -ftx.QuantityLiters
        ELSE 0 
    END), 0) 
    -- Note: We will later subtract DailyReadings consumed fuel in a complex query or TVF,
    -- but for simple transaction balance:
    AS TransactionBalanceLiters
FROM [dbo].[FuelTransactions] ftx
LEFT JOIN [dbo].[Lookups] tl ON tl.Id = ftx.TransactionTypeLookupId
WHERE ftx.DeletedAt IS NULL AND ftx.IsApproved = 1
GROUP BY ftx.SubStationId, ftx.FuelLookupId;
GO

-- View: Current oil balance per SubStation & OilType
CREATE OR ALTER VIEW [dbo].[v_SubStationOilStock] AS
SELECT 
    otx.SubStationId,
    otx.OilLookupId,
    ISNULL(SUM(CASE 
        WHEN tl.Code IN ('Purchase', 'TransferIn', 'AdjustmentAdd') THEN otx.QuantityLiters
        WHEN tl.Code IN ('Consumption', 'TransferOut', 'Loss', 'AdjustmentRemove') THEN -otx.QuantityLiters
        ELSE 0 
    END), 0) AS TransactionBalanceLiters
FROM [dbo].[OilTransactions] otx
LEFT JOIN [dbo].[Lookups] tl ON tl.Id = otx.TransactionTypeLookupId
WHERE otx.DeletedAt IS NULL AND otx.IsApproved = 1
GROUP BY otx.SubStationId, otx.OilLookupId;
GO
