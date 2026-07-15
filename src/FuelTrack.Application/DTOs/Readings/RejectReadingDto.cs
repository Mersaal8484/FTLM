namespace FuelTrack.Application.DTOs.Readings;

public record RejectReadingDto(
    string RejectionReason,
    int RejectedById);
