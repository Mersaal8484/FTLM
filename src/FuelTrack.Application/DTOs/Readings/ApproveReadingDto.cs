namespace FuelTrack.Application.DTOs.Readings;

public record ApproveReadingDto(
    int ApprovedById,
    string? VerificationCode);
