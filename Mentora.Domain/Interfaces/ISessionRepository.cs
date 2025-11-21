using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface ISessionRepository
{
    Task<Session?> GetByIdAsync(int id);
    Task<IEnumerable<Session>> GetByMentorIdAsync(string mentorId);
    Task<IEnumerable<Session>> GetAvailableSessionsAsync();
    Task<Session> CreateAsync(Session session);
    Task<Session> UpdateAsync(Session session);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<IEnumerable<Session>> GetRecurringInstancesAsync(int parentSessionId);
}
