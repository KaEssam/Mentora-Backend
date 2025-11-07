using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IBookingService
{
    Task<Booking?> GetBookingByIdAsync(string id);
    Task<IEnumerable<Booking>> GetBookingsByMentorAsync(string mentorId);
    Task<IEnumerable<Booking>> GetBookingsByMenteeAsync(string menteeId);
    Task<Booking> CreateBookingAsync(string sessionId, string menteeId);
    Task<Booking> UpdateBookingAsync(Booking booking);
    Task<bool> CancelBookingAsync(string bookingId, string userId);
    Task<bool> ConfirmBookingAsync(string bookingId, string mentorId);
    Task<bool> ValidateBooking(string sessionId, string menteeId);
    string GenerateMeetingUrl(Booking booking);
    Task<IEnumerable<Booking>> GetBookingsByStatusAsync(SessionStatus status);
}