namespace FuelTrack.Application.DTOs.FuelMovements;

public record OilStockSummaryDto(
    int SubStationId,
    string SubStationName,
    int StationId,
    string StationName,
    int BranchId,
    string BranchName,
    int OilLookupId,
    string OilLookupName,
    double BaseStock,
    double Inbound,
    double Outbound,
    double TransferOut,
    double TransferIn,
    double Loss,
    double Consumed,
    double RemainingStock,
    string AlertLevel);
