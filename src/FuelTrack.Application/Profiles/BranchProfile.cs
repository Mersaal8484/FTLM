using AutoMapper;
using FuelTrack.Application.DTOs.Branches;
using FuelTrack.Domain.Models;

namespace FuelTrack.Application.Profiles;

public class BranchProfile : Profile
{
    public BranchProfile()
    {
        CreateMap<Branch, BranchListDto>()
            .ConstructUsing(src => new BranchListDto(
                src.Id, src.Name, src.Code, src.IsGeneration,
                src.Stations != null ? src.Stations.Count(s => s.DeletedAt == null) : 0));

        CreateMap<Branch, BranchDetailDto>()
            .ConstructUsing(src => new BranchDetailDto(
                src.Id, src.Name, src.Code, src.IsGeneration, src.Notes,
                src.Stations != null ? src.Stations.Count(s => s.DeletedAt == null) : 0,
                src.CreatedAt, src.UpdatedAt));

        CreateMap<CreateBranchDto, Branch>();
        CreateMap<UpdateBranchDto, Branch>();
    }
}
