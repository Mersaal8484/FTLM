namespace FuelTrack.Application.DTOs.Readings;

public record ReadingFilterDto(
    int? GeneratorId,
    DateTime? DateFrom,
    DateTime? DateTo,
    int? ReadingStatusLookupId,
    string? Status,
    int Page = 1,
    int PageSize = 10);
