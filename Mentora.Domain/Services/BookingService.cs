using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Domain.Services;

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;

    public BookingService(
        IBookingRepository bookingRepository,
        ISessionRepository sessionRepository,
        IUserRepository userRepository)
    {
        _bookingRepository = bookingRepository;
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
    }

    public async Task<Booking?> GetBookingByIdAsync(string id)
    {
        return await _bookingRepository.GetByIdAsync(id);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByMentorAsync(string mentorId)
    {
        return await _bookingRepository.GetByMentorIdAsync(mentorId);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByMenteeAsync(string menteeId)
    {
        return await _bookingRepository.GetByMenteeIdAsync(menteeId);
    }

    public async Task<Booking> CreateBookingAsync(int sessionId, string menteeId)
    {
        // Validate session exists and is available
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException("Session not found.");
        }

        if (!await ValidateBooking(sessionId, menteeId))
        {
            throw new InvalidOperationException("Session is not available for booking.");
        }

        // Validate mentee exists
        var mentee = await _userRepository.GetByIdAsync(menteeId);
        if (mentee == null)
        {
            throw new ArgumentException("Mentee not found.");
        }

        // Check if mentee already has a booking for this session
        var existingBookings = await _bookingRepository.GetBySessionIdAsync(sessionId);
        if (existingBookings.Any(b => b.MenteeId == menteeId))
        {
            throw new InvalidOperationException("You have already booked this session.");
        }

        var booking = new Booking
        {
            Id = Guid.NewGuid().ToString(),
            SessionId = sessionId,
            MentorId = session.MentorId,
            MenteeId = menteeId,
            Status = SessionStatus.Pending,
            Type = session.Type,
            MeetingUrl = null // Will be generated when confirmed
        };

        return await _bookingRepository.CreateAsync(booking);
    }

    public async Task<Booking> UpdateBookingAsync(Booking booking)
    {
        var existingBooking = await _bookingRepository.GetByIdAsync(booking.Id);
        if (existingBooking == null)
        {
            throw new ArgumentException("Booking not found.");
        }

        return await _bookingRepository.UpdateAsync(booking);
    }

    public async Task<bool> CancelBookingAsync(string bookingId, string userId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        // Only mentee or mentor can cancel
        if (booking.MenteeId != userId && booking.MentorId != userId)
        {
            throw new UnauthorizedAccessException("You can only cancel your own bookings.");
        }

        // Cannot cancel confirmed sessions that are starting soon
        if (booking.Status == SessionStatus.Confirmed)
        {
            var session = await _sessionRepository.GetByIdAsync(booking.SessionId);
            if (session != null && session.StartAt <= DateTime.UtcNow.AddHours(2))
            {
                throw new InvalidOperationException("Cannot cancel booking less than 2 hours before session starts.");
            }
        }

        booking.Status = SessionStatus.Cancelled;
        await _bookingRepository.UpdateAsync(booking);
        return true;
    }

    public async Task<bool> ConfirmBookingAsync(string bookingId, string mentorId)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId);
        if (booking == null)
        {
            return false;
        }

        // Only mentor can confirm
        if (booking.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("Only the mentor can confirm bookings.");
        }

        if (booking.Status != SessionStatus.Pending)
        {
            throw new InvalidOperationException("Only pending bookings can be confirmed.");
        }

        booking.Status = SessionStatus.Confirmed;
        booking.MeetingUrl = GenerateMeetingUrl(booking);

        await _bookingRepository.UpdateAsync(booking);
        return true;
    }

    public async Task<bool> ValidateBooking(int sessionId, string menteeId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return false;

        if (session.Status != SessionStatus.Scheduled) return false;

        if (session.StartAt <= DateTime.UtcNow) return false;

        // Check if session is already booked
        var existingBookings = await _bookingRepository.GetBySessionIdAsync(sessionId);
        if (existingBookings.Any(b => b.Status == SessionStatus.Confirmed))
        {
            return false;
        }

        return true;
    }

    public string GenerateMeetingUrl(Booking booking)
    {
        // Generate a unique meeting URL
        var meetingId = Guid.NewGuid().ToString("N")[..8].ToUpper();
        return $"https://meet.mentora.com/{meetingId}-{booking.Id[..8]}";
    }

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(SessionStatus status)
    {
        return await _bookingRepository.GetBookingsByStatusAsync(status);
    }
}