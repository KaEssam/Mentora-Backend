using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mentora.Core.Data;
using Mentora.Domain.DTOs;

namespace Mentora.Domain.Interfaces;

public interface IBookingService
{
    Task<BookingDto> CreateBookingAsync(string userId, CreateBookingDto bookingDto);
    Task<BookingDto?> GetBookingByIdAsync(int bookingId, string userId);
    Task<IEnumerable<BookingListDto>> GetUserBookingsAsync(string userId);
    Task<IEnumerable<BookingListDto>> GetMentorBookingsAsync(string mentorId);
    Task<BookingDto> UpdateBookingAsync(int bookingId, string userId, UpdateBookingDto bookingDto);
    Task<BookingDto> CancelBookingAsync(int bookingId, string userId, CancelBookingDto cancelDto);
    Task<BookingDto> ConfirmBookingAsync(int bookingId, string userId);
    Task<BookingDto> CompleteBookingAsync(int bookingId, string userId);
    Task<bool> IsTimeSlotAvailableAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null);
    Task<BookingStatsDto> GetUserBookingStatsAsync(string userId);
    Task<BookingStatsDto> GetMentorBookingStatsAsync(string mentorId);
    Task<BookingConfirmationDto> GetBookingConfirmationAsync(int bookingId, string userId);
}
