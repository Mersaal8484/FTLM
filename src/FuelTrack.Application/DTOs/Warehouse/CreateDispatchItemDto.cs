namespace FuelTrack.Application.DTOs.Warehouse;

public record CreateDispatchItemDto(
    int SparePartId,
    decimal RequestedQuantity,
    string? Notes);

public record DispatchItemDetailDto(
    int Id,
    int SparePartId,
    string SparePartName,
    string PartNumber,
    string UnitName,
    decimal RequestedQuantity,
    decimal? ApprovedQuantity,
    decimal? IssuedQuantity,
    decimal AvailableInStock,
    bool IsApproved,
    string? Notes);

public record DispatchApprovalItemDto(
    int Id,
    string PartName,
    decimal RequestedQuantity,
    decimal AvailableQty);

public record ApproveDispatchRequestDto(
    List<ApproveDispatchItemDto> Items,
    string? Notes);

public record ApproveDispatchItemDto(
    int DispatchRequestItemId,
    decimal ApprovedQuantity,
    bool IsApproved);
