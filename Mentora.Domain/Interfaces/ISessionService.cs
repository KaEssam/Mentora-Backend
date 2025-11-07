using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface ISessionService
{
    Task<Session?> GetSessionByIdAsync(string id);
    Task<IEnumerable<Session>> GetSessionsByMentorAsync(string mentorId);
    Task<IEnumerable<Session>> GetAvailableSessionsAsync();
    Task<Session> CreateSessionAsync(Session session, string mentorId);
    Task<Session> UpdateSessionAsync(Session session, string mentorId);
    Task<bool> DeleteSessionAsync(string id, string mentorId);
    bool ValidateSessionTime(DateTime startTime, DateTime endTime);
    Task<bool> IsSessionAvailable(string sessionId);
    Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);
}