namespace FuelTrack.Application.DTOs.Warehouse;

public record CreateSparePartDto(
    string Name,
    string? NameEn,
    string PartNumber,
    string? ManufacturerPartNumber,
    string? Barcode,
    int PartCategoryLookupId,
    int UnitLookupId,
    string? Description,
    decimal UnitPrice,
    int MinStockQuantity,
    int? ReplacementIntervalHours,
    bool IsHoursTracked,
    int? ReplacementPriorityLookupId,
    string? Notes);
