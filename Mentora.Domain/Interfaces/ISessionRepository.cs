using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(string id);
    Task<IEnumerable<Session>> GetByMentorIdAsync(string mentorId);
    Task<IEnumerable<Session>> GetAvailableSessionsAsync();
    Task<Session> CreateAsync(Session session);
    Task<Session> UpdateAsync(Session session);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);
}