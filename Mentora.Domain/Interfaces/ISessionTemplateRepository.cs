using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface ISessionTemplateRepository
{
    Task<SessionTemplate?> GetByIdAsync(int id);
    Task<IEnumerable<SessionTemplate>> GetByMentorIdAsync(string mentorId);
    Task<IEnumerable<SessionTemplate>> GetActiveTemplatesByMentorIdAsync(string mentorId);
    Task<IEnumerable<SessionTemplate>> GetAllActiveTemplatesAsync();
    Task<SessionTemplate> CreateAsync(SessionTemplate template);
    Task<SessionTemplate> UpdateAsync(SessionTemplate template);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<SessionTemplate>> SearchTemplatesAsync(string searchTerm, SessionType? type = null);
    Task IncrementUsageCountAsync(int templateId);
    Task UpdateLastUsedAtAsync(int templateId);
    Task<IEnumerable<SessionTemplate>> GetPopularTemplatesAsync(int limit = 10);
}
