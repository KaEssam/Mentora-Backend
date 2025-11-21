using AutoMapper;
using Mentora.Domain.DTOs;
using Mentora.Core.Data;
using Mentora.Domain.Models;
using Mentora.Infra.Data;

namespace Mentora.APIs.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<ApplicationUser, UserProfileDto>()
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email ?? string.Empty));

        // Map SessionTemplate entities
        CreateMap<SessionTemplate, ResponseSessionTemplateDto>();

        CreateMap<CreateSessionTemplateDto, SessionTemplate>()
            .ForMember(dest => dest.DefaultRecurrenceJson, opt => opt.MapFrom(src =>
                src.DefaultRecurrence != null ? System.Text.Json.JsonSerializer.Serialize(src.DefaultRecurrence, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) : null));

        CreateMap<UpdateSessionTemplateDto, SessionTemplate>()
            .ForMember(dest => dest.DefaultRecurrenceJson, opt => opt.MapFrom(src =>
                src.DefaultRecurrence != null ? System.Text.Json.JsonSerializer.Serialize(src.DefaultRecurrence, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase }) : null));

        // If you need reverse mapping in the future
        // CreateMap<UserDto, ApplicationUser>();
        // CreateMap<UserProfileDto, ApplicationUser>();
        // CreateMap<ResponseSessionTemplateDto, SessionTemplate>();

        // Reminder mappings
        CreateMap<Reminder, ResponseReminderDto>();
        CreateMap<CreateReminderDto, Reminder>();
        CreateMap<UpdateReminderDto, Reminder>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<ReminderSettings, ReminderSettingsDto>();
        CreateMap<UpdateReminderSettingsDto, ReminderSettings>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        // Feedback mappings
        CreateMap<SessionFeedback, ResponseFeedbackDto>();
        CreateMap<CreateFeedbackDto, SessionFeedback>();
        CreateMap<UpdateFeedbackDto, SessionFeedback>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<FeedbackRating, FeedbackRatingDto>();

        // TODO: INTEGRATION - Advanced Mapping - Add complex mapping for nested objects when profile enhancement features are implemented
    }
}
