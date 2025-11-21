using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Interfaces.Repositories;

namespace Mentora.Domain.Services
{
    public class BookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISessionRepository _sessionRepository;

        public BookingService(IBookingRepository bookingRepository, ISessionRepository sessionRepository)
        {
            _bookingRepository = bookingRepository;
            _sessionRepository = sessionRepository;
        }

        public async Task<BookingDto> CreateBookingAsync(string userId, CreateBookingDto bookingDto)
        {
            // Validate session exists and is available
            if (!int.TryParse(bookingDto.SessionId, out var sessionIdValue))
            {
                throw new InvalidOperationException("Invalid session ID format");
            }
            var session = await _sessionRepository.GetByIdAsync(sessionIdValue);
            if (session == null)
            {
                throw new InvalidOperationException("Session not found");
            }

            // Check if user already has an active booking for this session
            var existingBooking = await _bookingRepository.GetActiveBookingForUserAndSessionAsync(userId, session.Id.ToString());
            if (existingBooking != null)
            {
                throw new InvalidOperationException("You already have an active booking for this session");
            }

            // Check if time slot is available for the mentor
            var isAvailable = await IsTimeSlotAvailableAsync(session.MentorId, bookingDto.SessionStartTime, bookingDto.SessionEndTime);
            if (!isAvailable)
            {
                throw new InvalidOperationException("The selected time slot is not available");
            }

            // Validate booking time
            if (bookingDto.SessionStartTime <= DateTime.UtcNow)
            {
                throw new InvalidOperationException("Booking time must be in the future");
            }

            if (bookingDto.SessionEndTime <= bookingDto.SessionStartTime)
            {
                throw new InvalidOperationException("End time must be after start time");
            }

            var booking = new Booking
            {
                UserId = userId,
                MentorId = session.MentorId,
                SessionId = session.Id.ToString(),
                BookingDate = DateTime.UtcNow,
                SessionStartTime = bookingDto.SessionStartTime,
                SessionEndTime = bookingDto.SessionEndTime,
                Amount = bookingDto.Amount,
                Currency = bookingDto.Currency,
                Status = BookingStatus.Pending,
                Notes = bookingDto.Notes
            };

            var createdBooking = await _bookingRepository.AddAsync(booking);

            return MapToBookingDto(createdBooking);
        }

        public async Task<BookingDto?> GetBookingByIdAsync(int bookingId, string userId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || (booking.UserId != userId && booking.MentorId != userId))
            {
                return null;
            }

            return MapToBookingDto(booking);
        }

        public async Task<IEnumerable<BookingListDto>> GetUserBookingsAsync(string userId)
        {
            var bookings = await _bookingRepository.GetByUserIdAsync(userId);
            return bookings.Select(MapToBookingListDto);
        }

        public async Task<IEnumerable<BookingListDto>> GetMentorBookingsAsync(string mentorId)
        {
            var bookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
            return bookings.Select(MapToBookingListDto);
        }

        public async Task<BookingDto> UpdateBookingAsync(int bookingId, string userId, UpdateBookingDto bookingDto)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found");
            }

            if (booking.UserId != userId && booking.MentorId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to update this booking");
            }

            if (booking.Status == BookingStatus.Completed || booking.Status == BookingStatus.Cancelled)
            {
                throw new InvalidOperationException("Cannot update completed or cancelled bookings");
            }

            // Update allowed fields
            if (bookingDto.Notes != null)
                booking.Notes = bookingDto.Notes;

            if (bookingDto.MeetingUrl != null)
                booking.MeetingUrl = bookingDto.MeetingUrl;

            if (bookingDto.IsPaid.HasValue)
            {
                booking.IsPaid = bookingDto.IsPaid.Value;
                if (bookingDto.IsPaid.Value && !booking.PaidAt.HasValue)
                {
                    booking.PaidAt = DateTime.UtcNow;
                }
            }

            if (bookingDto.PaymentIntentId != null)
            {
                booking.PaymentIntentId = bookingDto.PaymentIntentId;
            }

            var updatedBooking = await _bookingRepository.UpdateAsync(booking);
            return MapToBookingDto(updatedBooking);
        }

        public async Task<BookingDto> CancelBookingAsync(int bookingId, string userId, CancelBookingDto cancelDto)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found");
            }

            if (booking.UserId != userId && booking.MentorId != userId)
            {
                throw new UnauthorizedAccessException("You are not authorized to cancel this booking");
            }

            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            {
                throw new InvalidOperationException("Booking is already cancelled or completed");
            }

            // Check cancellation policy (basic check - will be enhanced in Commit 10)
            if (booking.SessionStartTime <= DateTime.UtcNow.AddHours(2))
            {
                throw new InvalidOperationException("Cannot cancel booking less than 2 hours before session time");
            }

            booking.Status = BookingStatus.Cancelled;
            booking.CancelReason = cancelDto.Reason;
            booking.CancelledAt = DateTime.UtcNow;

            var updatedBooking = await _bookingRepository.UpdateAsync(booking);
            return MapToBookingDto(updatedBooking);
        }

        public async Task<BookingDto> ConfirmBookingAsync(int bookingId, string userId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found");
            }

            if (booking.MentorId != userId)
            {
                throw new UnauthorizedAccessException("Only the mentor can confirm bookings");
            }

            if (booking.Status != BookingStatus.Pending)
            {
                throw new InvalidOperationException("Only pending bookings can be confirmed");
            }

            booking.Status = BookingStatus.Confirmed;
            var updatedBooking = await _bookingRepository.UpdateAsync(booking);
            return MapToBookingDto(updatedBooking);
        }

        public async Task<BookingDto> CompleteBookingAsync(int bookingId, string userId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                throw new InvalidOperationException("Booking not found");
            }

            if (booking.MentorId != userId)
            {
                throw new UnauthorizedAccessException("Only the mentor can complete bookings");
            }

            if (booking.Status != BookingStatus.Confirmed)
            {
                throw new InvalidOperationException("Only confirmed bookings can be completed");
            }

            if (booking.SessionEndTime > DateTime.UtcNow)
            {
                throw new InvalidOperationException("Cannot complete booking before session end time");
            }

            booking.Status = BookingStatus.Completed;
            var updatedBooking = await _bookingRepository.UpdateAsync(booking);
            return MapToBookingDto(updatedBooking);
        }

        public async Task<bool> IsTimeSlotAvailableAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            return await _bookingRepository.IsTimeSlotAvailableAsync(mentorId, startTime, endTime, excludeBookingId);
        }

        public async Task<BookingStatsDto> GetUserBookingStatsAsync(string userId)
        {
            var allBookings = await _bookingRepository.GetByUserIdAsync(userId);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var stats = new BookingStatsDto
            {
                TotalBookings = allBookings.Count(),
                PendingBookings = allBookings.Count(b => b.Status == BookingStatus.Pending),
                ConfirmedBookings = allBookings.Count(b => b.Status == BookingStatus.Confirmed),
                CompletedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed),
                CancelledBookings = allBookings.Count(b => b.Status == BookingStatus.Cancelled),
                TotalRevenue = allBookings.Where(b => b.IsPaid && b.Status == BookingStatus.Completed).Sum(b => b.Amount),
                MonthlyRevenue = allBookings.Where(b => b.IsPaid && b.Status == BookingStatus.Completed && b.CreatedAt >= monthStart).Sum(b => b.Amount)
            };

            return stats;
        }

        public async Task<BookingStatsDto> GetMentorBookingStatsAsync(string mentorId)
        {
            var allBookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1);

            var stats = new BookingStatsDto
            {
                TotalBookings = allBookings.Count(),
                PendingBookings = allBookings.Count(b => b.Status == BookingStatus.Pending),
                ConfirmedBookings = allBookings.Count(b => b.Status == BookingStatus.Confirmed),
                CompletedBookings = allBookings.Count(b => b.Status == BookingStatus.Completed),
                CancelledBookings = allBookings.Count(b => b.Status == BookingStatus.Cancelled),
                TotalRevenue = allBookings.Where(b => b.IsPaid && b.Status == BookingStatus.Completed).Sum(b => b.Amount),
                MonthlyRevenue = allBookings.Where(b => b.IsPaid && b.Status == BookingStatus.Completed && b.CreatedAt >= monthStart).Sum(b => b.Amount)
            };

            return stats;
        }

        public async Task<BookingConfirmationDto> GetBookingConfirmationAsync(int bookingId, string userId)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null || (booking.UserId != userId && booking.MentorId != userId))
            {
                throw new InvalidOperationException("Booking not found or access denied");
            }

            return new BookingConfirmationDto
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                MentorId = booking.MentorId,
                SessionStartTime = booking.SessionStartTime,
                SessionEndTime = booking.SessionEndTime,
                Amount = booking.Amount,
                Currency = booking.Currency,
                MeetingUrl = booking.MeetingUrl ?? string.Empty
            };
        }

        private static BookingDto MapToBookingDto(Booking booking)
        {
            return new BookingDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                MentorId = booking.MentorId,
                SessionId = booking.SessionId,
                BookingDate = booking.BookingDate,
                SessionStartTime = booking.SessionStartTime,
                SessionEndTime = booking.SessionEndTime,
                Amount = booking.Amount,
                Currency = booking.Currency,
                Status = booking.Status,
                Notes = booking.Notes,
                CreatedAt = booking.CreatedAt,
                UpdatedAt = booking.UpdatedAt,
                MeetingUrl = booking.MeetingUrl,
                CancelReason = booking.CancelReason,
                CancelledAt = booking.CancelledAt,
                IsPaid = booking.IsPaid,
                PaidAt = booking.PaidAt,
                PaymentIntentId = booking.PaymentIntentId
            };
        }

        private static BookingListDto MapToBookingListDto(Booking booking)
        {
            return new BookingListDto
            {
                Id = booking.Id,
                UserId = booking.UserId,
                MentorId = booking.MentorId,
                SessionId = booking.SessionId,
                SessionStartTime = booking.SessionStartTime,
                SessionEndTime = booking.SessionEndTime,
                Amount = booking.Amount,
                Currency = booking.Currency,
                Status = booking.Status,
                CreatedAt = booking.CreatedAt
            };
        }
    }
}
