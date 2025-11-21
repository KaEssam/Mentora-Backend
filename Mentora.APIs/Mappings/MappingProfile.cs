using AutoMapper;
using Mentora.APIs.DTOs;
using Mentora.Core.Data;
using Mentora.Domain.Models;

namespace Mentora.APIs.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // Map from ApplicationUser to UserProfileDto
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // If you need reverse mapping in the future
        // CreateMap<UserDto, ApplicationUser>();
        // CreateMap<UserProfileDto, ApplicationUser>();
        // TODO: INTEGRATION - Advanced Mapping - Add complex mapping for nested objects when profile enhancement features are implemented
    }
}
