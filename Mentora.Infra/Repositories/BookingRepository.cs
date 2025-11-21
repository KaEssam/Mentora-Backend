using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Interfaces.Repositories;
using Mentora.Infra.Data;

namespace Mentora.Infra.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly ApplicationDbContext _context;

        public BookingRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Booking?> GetByIdAsync(int id)
        {
            return await _context.Bookings

                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<IEnumerable<Booking>> GetAllAsync()
        {
            return await _context.Bookings

                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByUserIdAsync(string userId)
        {
            return await _context.Bookings

                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByMentorIdAsync(string mentorId)
        {
            return await _context.Bookings

                .Where(b => b.MentorId == mentorId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetBySessionIdAsync(string sessionId)
        {
            return await _context.Bookings

                .Where(b => b.SessionId == sessionId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetByStatusAsync(BookingStatus status)
        {
            return await _context.Bookings

                .Where(b => b.Status == status)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetUpcomingBookingsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Bookings

                .Where(b => b.UserId == userId &&
                           b.SessionStartTime > now &&
                           b.Status == BookingStatus.Confirmed)
                .OrderBy(b => b.SessionStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetUpcomingBookingsForMentorAsync(string mentorId)
        {
            var now = DateTime.UtcNow;
            return await _context.Bookings

                .Where(b => b.MentorId == mentorId &&
                           b.SessionStartTime > now &&
                           b.Status == BookingStatus.Confirmed)
                .OrderBy(b => b.SessionStartTime)
                .ToListAsync();
        }

        public async Task<IEnumerable<Booking>> GetPastBookingsAsync(string userId)
        {
            var now = DateTime.UtcNow;
            return await _context.Bookings

                .Where(b => b.UserId == userId &&
                           (b.SessionEndTime < now || b.Status == BookingStatus.Completed || b.Status == BookingStatus.Cancelled))
                .OrderByDescending(b => b.SessionStartTime)
                .ToListAsync();
        }

        public async Task<bool> IsTimeSlotAvailableAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            var query = _context.Bookings
                .Where(b => b.MentorId == mentorId &&
                           b.Status == BookingStatus.Confirmed &&
                           ((b.SessionStartTime < endTime && b.SessionEndTime > startTime) || // Overlapping time slots
                            (b.SessionStartTime >= startTime && b.SessionStartTime < endTime) ||
                            (b.SessionEndTime > startTime && b.SessionEndTime <= endTime)));

            if (excludeBookingId.HasValue)
            {
                query = query.Where(b => b.Id != excludeBookingId.Value);
            }

            return !await query.AnyAsync();
        }

        public async Task<Booking> AddAsync(Booking booking)
        {
            booking.CreatedAt = DateTime.UtcNow;
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            return booking;
        }

        public async Task<Booking> UpdateAsync(Booking booking)
        {
            booking.UpdatedAt = DateTime.UtcNow;

            _context.Bookings.Update(booking);
            await _context.SaveChangesAsync();

            return booking;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
                return false;

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> CountAsync()
        {
            return await _context.Bookings.CountAsync();
        }

        public async Task<int> CountByStatusAsync(BookingStatus status)
        {
            return await _context.Bookings.CountAsync(b => b.Status == status);
        }

        public async Task<decimal> GetTotalRevenueAsync(string? mentorId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.Bookings.Where(b => b.IsPaid && b.Status == BookingStatus.Completed);

            if (!string.IsNullOrEmpty(mentorId))
            {
                query = query.Where(b => b.MentorId == mentorId);
            }

            if (startDate.HasValue)
            {
                query = query.Where(b => b.SessionStartTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(b => b.SessionStartTime <= endDate.Value);
            }

            return await query.SumAsync(b => b.Amount);
        }

        public async Task<Booking?> GetActiveBookingForUserAndSessionAsync(string userId, string sessionId)
        {
            return await _context.Bookings

                .FirstOrDefaultAsync(b => b.UserId == userId &&
                                        b.SessionId == sessionId &&
                                        (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed));
        }
    }
}
