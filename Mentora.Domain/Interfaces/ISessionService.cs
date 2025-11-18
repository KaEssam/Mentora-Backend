using Mentora.Core.Data;
using Mentora.APIs.DTOs;

namespace Mentora.Domain.Interfaces;

public interface ISessionService
{
    Task<Session?> GetSessionByIdAsync(int id);
    Task<IEnumerable<Session>> GetSessionsByMentorAsync(string mentorId);
    Task<IEnumerable<Session>> GetAvailableSessionsAsync();
    Task<ResponseSessionDto> CreateSessionAsync(CreateSessionDto session, string mentorId);
    Task<Session> UpdateSessionAsync(Session session, string mentorId);
    Task<bool> DeleteSessionAsync(int id, string mentorId);
    bool ValidateSessionTime(DateTime startTime, DateTime endTime);
    Task<bool> IsSessionAvailable(int sessionId);
    Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);
}
