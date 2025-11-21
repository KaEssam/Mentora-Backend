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
    public interface IBookingValidationService
    {
        Task<BookingValidationResult> ValidateBookingRequestAsync(string userId, CreateBookingDto bookingDto);
        Task<BookingValidationResult> ValidateTimeSlotAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null);
        Task<BookingConflictResult> CheckBookingConflictsAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null);
        Task<MentorAvailabilityResult> GetMentorAvailabilityAsync(string mentorId, DateTime startDate, DateTime endDate);
        Task<List<TimeSlotSuggestion>> SuggestAvailableTimeSlotsAsync(string mentorId, DateTime preferredDate, int durationMinutes, int suggestionCount = 5);
        Task<bool> ValidateBookingCancellationAsync(int bookingId, string userId, string reason);
        Task<bool> ValidateBookingModificationAsync(int bookingId, string userId, UpdateBookingDto updateDto);
    }

    public class BookingValidationService : IBookingValidationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ISessionRepository _sessionRepository;
        private readonly IUserRepository _userRepository;

        public BookingValidationService(
            IBookingRepository bookingRepository,
            ISessionRepository sessionRepository,
            IUserRepository userRepository)
        {
            _bookingRepository = bookingRepository;
            _sessionRepository = sessionRepository;
            _userRepository = userRepository;
        }

        public async Task<BookingValidationResult> ValidateBookingRequestAsync(string userId, CreateBookingDto bookingDto)
        {
            var result = new BookingValidationResult { IsValid = true };

            try
            {
                // Validate user exists and is active
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("User not found");
                    return result;
                }

                // Validate session ID format and existence
                if (!int.TryParse(bookingDto.SessionId, out var sessionIdValue))
                {
                    result.IsValid = false;
                    result.Errors.Add("Invalid session ID format");
                    return result;
                }

                var session = await _sessionRepository.GetByIdAsync(sessionIdValue);
                if (session == null)
                {
                    result.IsValid = false;
                    result.Errors.Add("Session not found");
                    return result;
                }

                // Validate booking time constraints
                if (bookingDto.SessionStartTime <= DateTime.UtcNow)
                {
                    result.IsValid = false;
                    result.Errors.Add("Booking time must be in the future");
                }

                if (bookingDto.SessionEndTime <= bookingDto.SessionStartTime)
                {
                    result.IsValid = false;
                    result.Errors.Add("End time must be after start time");
                }

                var bookingDuration = bookingDto.SessionEndTime - bookingDto.SessionStartTime;
                if (bookingDuration.TotalMinutes < 15)
                {
                    result.IsValid = false;
                    result.Errors.Add("Minimum booking duration is 15 minutes");
                }

                if (bookingDuration.TotalHours > 8)
                {
                    result.IsValid = false;
                    result.Errors.Add("Maximum booking duration is 8 hours");
                }

                // Validate user doesn't already have an active booking for this session
                var existingBooking = await _bookingRepository.GetActiveBookingForUserAndSessionAsync(userId, session.Id.ToString());
                if (existingBooking != null)
                {
                    result.IsValid = false;
                    result.Errors.Add("You already have an active booking for this session");
                }

                // Check if time slot is available for the mentor
                var timeSlotValidation = await ValidateTimeSlotAsync(session.MentorId, bookingDto.SessionStartTime, bookingDto.SessionEndTime);
                if (!timeSlotValidation.IsValid)
                {
                    result.IsValid = false;
                    result.Errors.AddRange(timeSlotValidation.Errors);
                }

                // Validate amount and currency
                if (bookingDto.Amount <= 0)
                {
                    result.IsValid = false;
                    result.Errors.Add("Booking amount must be greater than 0");
                }

                if (string.IsNullOrWhiteSpace(bookingDto.Currency))
                {
                    result.IsValid = false;
                    result.Errors.Add("Currency is required");
                }

                // Validate mentor is available for bookings
                var mentorAvailability = await GetMentorAvailabilityAsync(session.MentorId, bookingDto.SessionStartTime, bookingDto.SessionEndTime);
                if (!mentorAvailability.IsAvailable)
                {
                    result.IsValid = false;
                    result.Errors.Add($"Mentor is not available during the requested time: {mentorAvailability.UnavailableReason}");
                }

                result.ValidatedSession = session;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Validation error: {ex.Message}");
            }

            return result;
        }

        public async Task<BookingValidationResult> ValidateTimeSlotAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            var result = new BookingValidationResult { IsValid = true };

            // Basic time validation
            if (startTime >= endTime)
            {
                result.IsValid = false;
                result.Errors.Add("Start time must be before end time");
                return result;
            }

            if (startTime <= DateTime.UtcNow.AddMinutes(15))
            {
                result.IsValid = false;
                result.Errors.Add("Booking must be made at least 15 minutes in advance");
                return result;
            }

            // Check for conflicts with existing bookings
            var conflictResult = await CheckBookingConflictsAsync(mentorId, startTime, endTime, excludeBookingId);
            if (conflictResult.HasConflicts)
            {
                result.IsValid = false;
                result.Errors.Add($"Time slot conflicts with existing bookings: {string.Join(", ", conflictResult.ConflictingBookings.Select(b => b.Id))}");
            }

            // Validate business hours (9 AM - 9 PM, Monday-Friday)
            if (IsOutsideBusinessHours(startTime) || IsOutsideBusinessHours(endTime))
            {
                result.IsValid = false;
                result.Errors.Add("Booking must be within business hours (9 AM - 9 PM, Monday-Friday)");
            }

            // Validate booking doesn't span more than one day
            if (startTime.Date != endTime.Date)
            {
                result.IsValid = false;
                result.Errors.Add("Booking must be within the same day");
            }

            return result;
        }

        public async Task<BookingConflictResult> CheckBookingConflictsAsync(string mentorId, DateTime startTime, DateTime endTime, int? excludeBookingId = null)
        {
            var result = new BookingConflictResult();

            // Get all confirmed bookings for the mentor in the time range
            var mentorBookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
            var conflictingBookings = mentorBookings
                .Where(b => b.Status == BookingStatus.Confirmed &&
                           (excludeBookingId == null || b.Id != excludeBookingId.Value))
                .Where(b => IsTimeSlotOverlapping(startTime, endTime, b.SessionStartTime, b.SessionEndTime))
                .ToList();

            result.HasConflicts = conflictingBookings.Any();
            result.ConflictingBookings = conflictingBookings;

            return result;
        }

        public async Task<MentorAvailabilityResult> GetMentorAvailabilityAsync(string mentorId, DateTime startDate, DateTime endDate)
        {
            var result = new MentorAvailabilityResult { IsAvailable = true };

            try
            {
                // Validate mentor exists
                var mentor = await _userRepository.GetByIdAsync(mentorId);
                if (mentor == null)
                {
                    result.IsAvailable = false;
                    result.UnavailableReason = "Mentor not found";
                    return result;
                }

                // Get all bookings for the mentor in the date range
                var mentorBookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
                var dateRangeBookings = mentorBookings
                    .Where(b => b.Status == BookingStatus.Confirmed &&
                               b.SessionStartTime >= startDate && b.SessionEndTime <= endDate)
                    .ToList();

                result.BookedSlots = dateRangeBookings;

                // Check if mentor has too many bookings (max 8 per day)
                var bookingsPerDay = dateRangeBookings
                    .GroupBy(b => b.SessionStartTime.Date)
                    .ToDictionary(g => g.Key, g => g.Count());

                foreach (var dayBookings in bookingsPerDay)
                {
                    if (dayBookings.Value >= 8)
                    {
                        result.IsAvailable = false;
                        result.UnavailableReason = $"Mentor has reached maximum bookings limit for {dayBookings.Key:yyyy-MM-dd}";
                        return result;
                    }
                }

                // Check total hours per day (max 8 hours)
                var hoursPerDay = dateRangeBookings
                    .GroupBy(b => b.SessionStartTime.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(b => (b.SessionEndTime - b.SessionStartTime).TotalHours));

                foreach (var dayHours in hoursPerDay)
                {
                    if (dayHours.Value >= 8)
                    {
                        result.IsAvailable = false;
                        result.UnavailableReason = $"Mentor has reached maximum working hours for {dayHours.Key:yyyy-MM-dd}";
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.IsAvailable = false;
                result.UnavailableReason = $"Error checking availability: {ex.Message}";
            }

            return result;
        }

        public async Task<List<TimeSlotSuggestion>> SuggestAvailableTimeSlotsAsync(string mentorId, DateTime preferredDate, int durationMinutes, int suggestionCount = 5)
        {
            var suggestions = new List<TimeSlotSuggestion>();
            var duration = TimeSpan.FromMinutes(durationMinutes);

            try
            {
                // Get existing bookings for the mentor on the preferred date
                var existingBookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
                var dayBookings = existingBookings
                    .Where(b => b.Status == BookingStatus.Confirmed &&
                               b.SessionStartTime.Date == preferredDate.Date)
                    .OrderBy(b => b.SessionStartTime)
                    .ToList();

                // Define business hours (9 AM - 9 PM)
                var businessStart = preferredDate.Date.AddHours(9);
                var businessEnd = preferredDate.Date.AddHours(21);

                // Generate potential time slots
                var currentTime = businessStart;
                var slotIndex = 0;

                while (currentTime.Add(duration) <= businessEnd && suggestions.Count < suggestionCount)
                {
                    var slotEnd = currentTime.Add(duration);

                    // Check if this time slot conflicts with existing bookings
                    var hasConflict = dayBookings.Any(b =>
                        IsTimeSlotOverlapping(currentTime, slotEnd, b.SessionStartTime, b.SessionEndTime));

                    if (!hasConflict && currentTime > DateTime.UtcNow.AddMinutes(15))
                    {
                        suggestions.Add(new TimeSlotSuggestion
                        {
                            StartTime = currentTime,
                            EndTime = slotEnd,
                            Duration = duration,
                            Score = CalculateSuggestionScore(currentTime, preferredDate, slotIndex),
                            Reason = "Available time slot"
                        });
                    }

                    // Move to next slot (30-minute intervals)
                    currentTime = currentTime.AddMinutes(30);
                    slotIndex++;
                }

                // If no slots found on preferred date, suggest next available day
                if (!suggestions.Any())
                {
                    var nextDay = preferredDate.Date.AddDays(1);
                    while (nextDay.DayOfWeek == DayOfWeek.Saturday || nextDay.DayOfWeek == DayOfWeek.Sunday)
                    {
                        nextDay = nextDay.AddDays(1);
                    }

                    suggestions.Add(new TimeSlotSuggestion
                    {
                        StartTime = nextDay.AddHours(9),
                        EndTime = nextDay.AddHours(9).Add(duration),
                        Duration = duration,
                        Score = 0.5,
                        Reason = "Next available business day"
                    });
                }
            }
            catch (Exception ex)
            {
                // In case of error, return a basic suggestion
                suggestions.Add(new TimeSlotSuggestion
                {
                    StartTime = preferredDate.Date.AddDays(1).AddHours(9),
                    EndTime = preferredDate.Date.AddDays(1).AddHours(9).Add(duration),
                    Duration = duration,
                    Score = 0.3,
                    Reason = "Fallback suggestion due to error"
                });
            }

            return suggestions.Take(suggestionCount).OrderByDescending(s => s.Score).ToList();
        }

        public async Task<bool> ValidateBookingCancellationAsync(int bookingId, string userId, string reason)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                    return false;

                // Check if user owns this booking
                if (booking.UserId != userId && booking.MentorId != userId)
                    return false;

                // Check if booking can be cancelled
                if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                    return false;

                // Check cancellation policy (at least 2 hours before session)
                if (booking.SessionStartTime <= DateTime.UtcNow.AddHours(2))
                    return false;

                // Validate cancellation reason
                if (string.IsNullOrWhiteSpace(reason) || reason.Length < 10)
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> ValidateBookingModificationAsync(int bookingId, string userId, UpdateBookingDto updateDto)
        {
            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                    return false;

                // Check if user owns this booking
                if (booking.UserId != userId && booking.MentorId != userId)
                    return false;

                // Check if booking can be modified
                if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                    return false;

                // Check if modification is before the session starts
                if (booking.SessionStartTime <= DateTime.UtcNow.AddMinutes(30))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsTimeSlotOverlapping(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
        {
            return start1 < end2 && end1 > start2;
        }

        private static bool IsOutsideBusinessHours(DateTime time)
        {
            return time.Hour < 9 || time.Hour >= 21 || time.DayOfWeek == DayOfWeek.Saturday || time.DayOfWeek == DayOfWeek.Sunday;
        }

        private static double CalculateSuggestionScore(DateTime suggestedTime, DateTime preferredTime, int slotIndex)
        {
            var score = 1.0;

            // Penalize slots far from preferred time
            var timeDiff = Math.Abs((suggestedTime - preferredTime).TotalHours);
            score -= Math.Min(timeDiff * 0.1, 0.5);

            // Prefer earlier slots
            score -= slotIndex * 0.05;

            // Prefer morning slots
            if (suggestedTime.Hour >= 9 && suggestedTime.Hour <= 11)
                score += 0.1;

            return Math.Max(score, 0.1);
        }
    }

    // Result classes
    public class BookingValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public Session? ValidatedSession { get; set; }
    }

    public class BookingConflictResult
    {
        public bool HasConflicts { get; set; }
        public List<Booking> ConflictingBookings { get; set; } = new List<Booking>();
    }

    public class MentorAvailabilityResult
    {
        public bool IsAvailable { get; set; }
        public string? UnavailableReason { get; set; }
        public List<Booking> BookedSlots { get; set; } = new List<Booking>();
    }

    public class TimeSlotSuggestion
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public double Score { get; set; }
        public string? Reason { get; set; }
    }
}
