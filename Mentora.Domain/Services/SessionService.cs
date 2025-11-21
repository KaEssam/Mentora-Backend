using Mentora.Domain.DTOs;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Interfaces.Repositories;

namespace Mentora.Domain.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRecurrenceService _recurrenceService;

    public SessionService(ISessionRepository sessionRepository, IBookingRepository bookingRepository, IRecurrenceService recurrenceService)
    {
        _sessionRepository = sessionRepository;
        _bookingRepository = bookingRepository;
        _recurrenceService = recurrenceService;
    }

    public async Task<Session?> GetSessionByIdAsync(int id)
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

    public async Task<ResponseSessionDto> CreateSessionAsync(CreateSessionDto session, string mentorId)
    {
        if (session.StartAt <= DateTime.UtcNow)
        {
            throw new ArgumentException("Session start time must be in the future.");
        }

        if (await checkOverLap(mentorId, session.StartAt, session.StartAt.AddMinutes(30)))
        {
            throw new InvalidOperationException("cannot create session, slot existing");
        }

        var newSession = new Session
        {
            StartAt = session.StartAt,
            EndAt = session.StartAt.AddMinutes(30),
            Price = session.Price,
            MentorId = mentorId,
            Status = SessionStatus.Scheduled,
            Type = session.Type ?? SessionType.OneOnOne,
            Notes = session.Notes
        };

        // Handle recurrence
        if (session.IsRecurring && session.Recurrence != null)
        {
            newSession.IsRecurring = true;
            newSession.RecurrenceJson = _recurrenceService.SerializeRecurrence(session.Recurrence);

            var result = await _sessionRepository.CreateAsync(newSession);

            // Create recurring instances
            await CreateRecurringInstances(result, session.Recurrence);

            return MapToResponseDto(result);
        }

        var res = await _sessionRepository.CreateAsync(newSession);
        return MapToResponseDto(res);
    }

    public async Task<List<ResponseSessionDto>> CreateRecurringSessionAsync(CreateRecurringSessionDto sessionDto, string mentorId)
    {
        if (sessionDto.StartDate <= DateTime.UtcNow)
        {
            throw new ArgumentException("Session start date must be in the future.");
        }

        var parentSession = new Session
        {
            MentorId = mentorId,
            Status = SessionStatus.Scheduled,
            Type = sessionDto.Type ?? SessionType.OneOnOne,
            Price = sessionDto.Price,
            Notes = sessionDto.Notes,
            IsRecurring = true,
            RecurrenceJson = _recurrenceService.SerializeRecurrence(sessionDto.Recurrence)
        };

        var createdParent = await _sessionRepository.CreateAsync(parentSession);
        var createdInstances = await CreateRecurringInstances(createdParent, sessionDto.Recurrence, sessionDto.StartDate, sessionDto.StartTime, sessionDto.Duration);

        var allSessions = new List<Session> { createdParent };
        allSessions.AddRange(createdInstances);

        return allSessions.Select(MapToResponseDto).ToList();
    }

    public async Task<Session> UpdateSessionAsync(Session session, string mentorId)
    {
        var existingSession = await _sessionRepository.GetByIdAsync(session.Id);
        if (existingSession == null || existingSession.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only update your own sessions.");
        }

        if (session.StartAt != existingSession.StartAt || session.EndAt != existingSession.EndAt)
        {
            if (!ValidateSessionTime(session.StartAt, session.EndAt))
            {
                throw new ArgumentException("Invalid session time.");
            }

            var bookings = await _bookingRepository.GetBySessionIdAsync(session.Id.ToString());
            if (bookings.Any(b => b.Status == BookingStatus.Confirmed))
            {
                throw new InvalidOperationException("Cannot update session time when there are confirmed bookings.");
            }
        }

        return await _sessionRepository.UpdateAsync(session);
    }

    public async Task<bool> DeleteSessionAsync(int id, string mentorId)
    {
        var session = await _sessionRepository.GetByIdAsync(id);
        if (session == null || session.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only delete your own sessions.");
        }

        var bookings = await _bookingRepository.GetBySessionIdAsync(id.ToString());
        if (bookings.Any(b => b.Status == BookingStatus.Confirmed))
        {
            throw new InvalidOperationException("Cannot delete session with confirmed bookings.");
        }

        // If this is a parent session, also delete all recurring instances
        if (session.IsRecurring && session.ParentSessionId == null)
        {
            var instances = await _sessionRepository.GetRecurringInstancesAsync(id);
            foreach (var instance in instances)
            {
                await _sessionRepository.DeleteAsync(instance.Id);
            }
        }

        return await _sessionRepository.DeleteAsync(id);
    }

    public async Task<List<ResponseSessionDto>> GetRecurringSessionInstancesAsync(int parentSessionId)
    {
        var instances = await _sessionRepository.GetRecurringInstancesAsync(parentSessionId);
        return instances.Select(MapToResponseDto).ToList();
    }

    public bool ValidateSessionTime(DateTime startTime, DateTime endTime)
    {
        if (startTime >= endTime) return false;
        if ((endTime - startTime).TotalMinutes < 30) return false;
        if ((endTime - startTime).TotalHours > 4) return false;
        if (startTime <= DateTime.UtcNow) return false;
        return true;
    }

    public async Task<bool> IsSessionAvailable(int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null || session.Status != SessionStatus.Scheduled) return false;

        var bookings = await _bookingRepository.GetBySessionIdAsync(sessionId.ToString());
        return !bookings.Any(b => b.Status == BookingStatus.Confirmed);
    }

    public async Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _sessionRepository.GetSessionsByDateRangeAsync(startDate, endDate);
    }

    private async Task<List<Session>> CreateRecurringInstances(Session parentSession, RecurrenceDetails recurrence, DateTime? startDate = null, TimeSpan? startTime = null, TimeSpan? duration = null)
    {
        var instances = new List<Session>();
        var sessionStart = startDate ?? parentSession.StartAt;
        var sessionStartTime = startTime ?? parentSession.StartAt.TimeOfDay;
        var sessionDuration = duration ?? (parentSession.EndAt - parentSession.StartAt);

        var recurringDates = _recurrenceService.GenerateRecurringDates(sessionStart, recurrence);

        foreach (var date in recurringDates)
        {
            var sessionDateTime = date.Date.Add(sessionStartTime);

            if (sessionDateTime <= DateTime.UtcNow)
                continue;

            if (await checkOverLap(parentSession.MentorId, sessionDateTime, sessionDateTime + sessionDuration))
                continue;

            var instance = new Session
            {
                MentorId = parentSession.MentorId,
                StartAt = sessionDateTime,
                EndAt = sessionDateTime + sessionDuration,
                Status = SessionStatus.Scheduled,
                Type = parentSession.Type,
                Price = parentSession.Price,
                Notes = parentSession.Notes,
                IsRecurring = false, // Instances are not themselves recurring
                ParentSessionId = parentSession.Id
            };

            var createdInstance = await _sessionRepository.CreateAsync(instance);
            instances.Add(createdInstance);
        }

        return instances;
    }

    private ResponseSessionDto MapToResponseDto(Session session)
    {
        var dto = new ResponseSessionDto
        {
            Id = session.Id,
            MentorId = session.MentorId,
            StartAt = session.StartAt,
            EndAt = session.EndAt,
            Price = session.Price,
            Notes = session.Notes,
            Status = session.Status,
            Type = session.Type,
            IsRecurring = session.IsRecurring,
            ParentSessionId = session.ParentSessionId
        };

        if (!string.IsNullOrEmpty(session.RecurrenceJson))
        {
            dto.Recurrence = _recurrenceService.DeserializeRecurrence(session.RecurrenceJson);
        }

        return dto;
    }

    private async Task<bool> checkOverLap(string mentorId, DateTime startAt, DateTime endAt)
    {
        var mentorSessions = await _sessionRepository.GetByMentorIdAsync(mentorId);
        return mentorSessions.Any(existedSession =>
            (startAt >= existedSession.StartAt && startAt < existedSession.EndAt ||
             endAt > existedSession.StartAt && endAt <= existedSession.EndAt ||
             startAt <= existedSession.StartAt && endAt >= existedSession.EndAt));
    }
}
