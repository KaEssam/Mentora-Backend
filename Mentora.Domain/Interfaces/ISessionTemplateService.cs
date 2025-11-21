using Mentora.Core.Data;
using Mentora.Domain.DTOs;

namespace Mentora.Domain.Interfaces;

public interface ISessionTemplateService
{
    Task<ResponseSessionTemplateDto> CreateTemplateAsync(CreateSessionTemplateDto templateDto, string mentorId);
    Task<ResponseSessionTemplateDto> UpdateTemplateAsync(int id, UpdateSessionTemplateDto templateDto, string mentorId);
    Task<bool> DeleteTemplateAsync(int id, string mentorId);
    Task<ResponseSessionTemplateDto?> GetTemplateByIdAsync(int id);
    Task<IEnumerable<ResponseSessionTemplateDto>> GetTemplatesByMentorAsync(string mentorId);
    Task<IEnumerable<ResponseSessionTemplateDto>> GetActiveTemplatesByMentorAsync(string mentorId);
    Task<IEnumerable<ResponseSessionTemplateDto>> GetAllActiveTemplatesAsync();
    Task<IEnumerable<ResponseSessionTemplateDto>> SearchTemplatesAsync(string searchTerm, SessionType? type = null);
    Task<ResponseSessionDto> CreateSessionFromTemplateAsync(CreateSessionFromTemplateDto createDto, string mentorId);
    Task<List<ResponseSessionDto>> CreateRecurringSessionFromTemplateAsync(CreateSessionFromTemplateDto createDto, string mentorId);
    Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync(int templateId, string mentorId);
    Task<IEnumerable<TemplateUsageStatsDto>> GetAllTemplateUsageStatsAsync(string mentorId);
    Task<IEnumerable<ResponseSessionTemplateDto>> GetPopularTemplatesAsync(int limit = 10);
}
