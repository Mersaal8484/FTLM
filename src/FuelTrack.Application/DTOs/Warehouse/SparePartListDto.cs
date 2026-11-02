namespace FuelTrack.Application.DTOs.Warehouse;

public record SparePartListDto(
    int Id,
    string PartNumber,
    string Name,
    string PartCategoryName,
    string UnitName,
    int MinStockQuantity,
    int? ReplacementIntervalHours,
    bool IsHoursTracked,
    decimal UnitPrice,
    string? Notes,
    bool IsActive);

public record SparePartDetailDto(
    int Id,
    string PartNumber,
    string Name,
    string? NameEn,
    string? ManufacturerPartNumber,
    string? Barcode,
    int PartCategoryLookupId,
    string PartCategoryName,
    int UnitLookupId,
    string UnitName,
    string? Description,
    decimal UnitPrice,
    int MinStockQuantity,
    int? ReplacementIntervalHours,
    bool IsHoursTracked,
    int? ReplacementPriorityLookupId,
    string? ReplacementPriorityName,
    string? Notes,
    DateTime CreatedAt);
