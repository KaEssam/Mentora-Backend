using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IBookingRepository
{
    Task<Booking?> GetByIdAsync(string id);
    Task<IEnumerable<Booking>> GetByMentorIdAsync(string mentorId);
    Task<IEnumerable<Booking>> GetByMenteeIdAsync(string menteeId);
    Task<IEnumerable<Booking>> GetBySessionIdAsync(string sessionId);
    Task<Booking> CreateAsync(Booking booking);
    Task<Booking> UpdateAsync(Booking booking);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<IEnumerable<Booking>> GetBookingsByStatusAsync(SessionStatus status);
}