using AutoMapper;
using FuelTrack.Application.DTOs.SubStations;
using FuelTrack.Domain.Models;

namespace FuelTrack.Application.Profiles;

public class SubStationProfile : Profile
{
    public SubStationProfile()
    {
        CreateMap<SubStation, SubStationListDto>()
            .ConstructUsing(src => new SubStationListDto(
                src.Id,
                src.StationId,
                src.Station != null ? src.Station.Name : string.Empty,
                src.Name,
                src.Code,
                src.Location,
                src.CapacityKW,
                src.IsActive));

        CreateMap<SubStation, SubStationDetailDto>()
            .ConstructUsing(src => new SubStationDetailDto(
                src.Id,
                src.StationId,
                src.Station != null ? src.Station.Name : string.Empty,
                src.Name,
                src.Code,
                src.Location,
                src.CapacityKW,
                src.IsActive,
                src.CreatedAt,
                src.UpdatedAt));

        CreateMap<CreateSubStationDto, SubStation>();
        CreateMap<UpdateSubStationDto, SubStation>();
    }
}
