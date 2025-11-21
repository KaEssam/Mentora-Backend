using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces.Repositories
{
    public interface IBookingRepository
    {
        Task<Booking?> GetByIdAsync(int id);
        Task<IEnumerable<Booking>> GetAllAsync();
        Task<IEnumerable<Booking>> GetByUserIdAsync(string userId);
        Task<IEnumerable<Booking>> GetByMentorIdAsync(string mentorId);
        Task<IEnumerable<Booking>> GetBySessionIdAsync(string sessionId);
        Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status);
        Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string userId);
        Task<IEnumerable<Booking>> GetUpcomingBookingsForMentorAsync(string mentorId);
        Task<IEnumerable<Booking>> GetPastBookingsAsync(string userId);
        Task<bool> IsTimeSlotAvailableAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null);
        Task<Booking> AddAsync(Booking booking);
        Task<Booking> UpdateAsync(Booking booking);
        Task<bool> DeleteAsync(int id);
        Task<int> CountAsync();
        Task<int> CountByStatusAsync(BookingStatus status);
        Task<decimal> GetTotalRevenueAsync(string? mentorId = null, DateTime? startDate = null, DateTime? endDate = null);
        Task<Booking?> GetActiveBookingForUserAndSessionAsync(string userId, string sessionId);
    }
}
