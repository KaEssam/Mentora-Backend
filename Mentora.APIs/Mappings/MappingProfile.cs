using AutoMapper;
using Mentora.Core.Data;
using Mentora.APIs.DTOs;
using Mentora.Infra.Data;

namespace Mentora.APIs.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // If you need reverse mapping in the future
        // CreateMap<UserDto, ApplicationUser>();

        // Map from domain entity to DTO
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));
    }
}