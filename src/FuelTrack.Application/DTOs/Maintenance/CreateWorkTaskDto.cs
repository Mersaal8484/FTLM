namespace FuelTrack.Application.DTOs.Maintenance;

public record CreateWorkTaskDto(
    string TaskDescription,
    int SortOrder);

public record WorkTaskDto(
    int Id,
    string TaskDescription,
    int SortOrder,
    bool IsCompleted,
    DateTime? CompletedAt,
    int? CompletedById,
    string? CompletedByName);
