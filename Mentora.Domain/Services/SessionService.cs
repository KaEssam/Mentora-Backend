using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Domain.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IBookingRepository _bookingRepository;

    public SessionService(ISessionRepository sessionRepository, IBookingRepository bookingRepository)
    {
        _sessionRepository = sessionRepository;
        _bookingRepository = bookingRepository;
    }

    public async Task<Session?> GetSessionByIdAsync(string id)
    {
        return await _sessionRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Session>> GetSessionsByMentorAsync(string mentorId)
    {
        return await _sessionRepository.GetByMentorIdAsync(mentorId);
    }

    public async Task<IEnumerable<Session>> GetAvailableSessionsAsync()
    {
        var allSessions = await _sessionRepository.GetAvailableSessionsAsync();
        return allSessions.Where(s => s.Status == SessionStatus.Scheduled && s.StartAt > DateTime.UtcNow);
    }

    public async Task<Session> CreateSessionAsync(Session session, string mentorId)
    {
        // Business validation
        if (!ValidateSessionTime(session.StartAt, session.EndAt))
        {
            throw new ArgumentException("Invalid session time. End time must be after start time and session must be at least 30 minutes.");
        }

        if (session.StartAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Session start time must be in the future.");
        }

        session.MentorId = mentorId;
        session.Status = SessionStatus.Scheduled;

        return await _sessionRepository.CreateAsync(session);
    }

    public async Task<Session> UpdateSessionAsync(Session session, string mentorId)
    {
        // Validate ownership
        var existingSession = await _sessionRepository.GetByIdAsync(session.Id);
        if (existingSession == null || existingSession.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only update your own sessions.");
        }

        // Business validation for time updates
        if (session.StartAt != existingSession.StartAt || session.EndAt != existingSession.EndAt)
        {
            if (!ValidateSessionTime(session.StartAt, session.EndAt))
            {
                throw new ArgumentException("Invalid session time.");
            }

            // Check if session has bookings
            var bookings = await _bookingRepository.GetBySessionIdAsync(session.Id);
            if (bookings.Any(b => b.Status == SessionStatus.Confirmed))
            {
                throw new InvalidOperationException("Cannot update session time when there are confirmed bookings.");
            }
        }

        return await _sessionRepository.UpdateAsync(session);
    }

    public async Task<bool> DeleteSessionAsync(string id, string mentorId)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null || session.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only delete your own sessions.");
        }

        // Check if session has confirmed bookings
        var bookings = await _bookingRepository.GetBySessionIdAsync(id);
        if (bookings.Any(b => b.Status == SessionStatus.Confirmed))
        {
            throw new InvalidOperationException("Cannot delete session with confirmed bookings.");
        }

        return await _sessionRepository.DeleteAsync(id);
    }

    public bool ValidateSessionTime(DateTime startTime, DateTime endTime)
    {
        // Basic validation
        if (startTime >= endTime) return false;

        // Session must be at least 30 minutes
        if ((endTime - startTime).TotalMinutes < 30) return false;

        // Session cannot be longer than 4 hours
        if ((endTime - startTime).TotalHours > 4) return false;

        // Session must be in the future
        if (startTime <= DateTime.UtcNow) return false;

        return true;
    }

    public async Task<bool> IsSessionAvailable(string sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.Status != SessionStatus.Scheduled) return false;

        var bookings = await _bookingRepository.GetBySessionIdAsync(sessionId);
        return !bookings.Any(b => b.Status == SessionStatus.Confirmed);
    }

    public async Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _sessionRepository.GetSessionsByDateRangeAsync(startDate, endDate);
    }
}