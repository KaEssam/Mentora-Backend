using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Interfaces.Repositories;
using Mentora.Domain.Models;

namespace Mentora.Domain.Services
{
    public interface IMeetingIntegrationService
    {
        Task<MeetingUrlResult> GenerateMeetingUrlAsync(Booking booking);
        Task<MeetingUrlResult> RegenerateMeetingUrlAsync(int bookingId, string userId);
        Task<MeetingJoinResult> GetMeetingJoinInfoAsync(int bookingId, string userId);
        Task<MeetingStatusResult> GetMeetingStatusAsync(string meetingId);
        Task<List<MeetingParticipantResult>> GetMeetingParticipantsAsync(string meetingId);
        Task<MeetingRecordingResult> StartMeetingRecordingAsync(string meetingId);
        Task<MeetingRecordingResult> StopMeetingRecordingAsync(string meetingId);
        Task<bool> EndMeetingAsync(string meetingId, string userId);
        Task<List<MeetingSessionResult>> GetUpcomingMeetingsAsync(string userId);
        Task<MeetingAnalyticsResult> GetMeetingAnalyticsAsync(string mentorId, DateTime? startDate = null, DateTime? endDate = null);
        Task<MeetingSettingsResult> GetMeetingSettingsAsync();
        Task<MeetingSettingsResult> UpdateMeetingSettingsAsync(MeetingSettingsDto settings);
    }

    public class MeetingIntegrationService : IMeetingIntegrationService
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly IUserRepository _userRepository;

        public MeetingIntegrationService(
            IBookingRepository bookingRepository,
            IUserRepository userRepository)
        {
            _bookingRepository = bookingRepository;
            _userRepository = userRepository;
        }

        public async Task<MeetingUrlResult> GenerateMeetingUrlAsync(Booking booking)
        {
            var result = new MeetingUrlResult();

            try
            {
                // Validate booking can have a meeting
                if (booking.Status != BookingStatus.Confirmed)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting URL can only be generated for confirmed bookings";
                    return result;
                }

                // Check if meeting is in the future (not too early to generate)
                if (booking.SessionStartTime > DateTime.UtcNow.AddHours(24))
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting URL can only be generated within 24 hours of the session";
                    return result;
                }

                // Generate unique meeting ID
                var meetingId = GenerateMeetingId(booking.Id, booking.UserId, booking.MentorId);

                // Generate meeting URL using a mock integration (in real implementation, this would call Zoom/Teams/Meet API)
                var meetingUrl = await GenerateExternalMeetingUrl(meetingId, booking);

                // Update booking with meeting URL
                if (!string.IsNullOrEmpty(meetingUrl))
                {
                    booking.MeetingUrl = meetingUrl;
                    await _bookingRepository.UpdateAsync(booking);
                }

                result.IsSuccess = true;
                result.MeetingId = meetingId;
                result.MeetingUrl = meetingUrl;
                result.JoinUrl = GenerateJoinUrl(meetingId, booking.UserId);
                result.HostUrl = GenerateHostUrl(meetingId, booking.MentorId);
                result.StartTime = booking.SessionStartTime;
                result.EndTime = booking.SessionEndTime;
                result.DurationMinutes = (int)(booking.SessionEndTime - booking.SessionStartTime).TotalMinutes;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error generating meeting URL: {ex.Message}";
            }

            return result;
        }

        public async Task<MeetingUrlResult> RegenerateMeetingUrlAsync(int bookingId, string userId)
        {
            var result = new MeetingUrlResult();

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Booking not found";
                    return result;
                }

                // Check authorization
                if (booking.UserId != userId && booking.MentorId != userId)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "You are not authorized to modify this booking's meeting URL";
                    return result;
                }

                // Generate new meeting URL
                var newMeetingId = GenerateMeetingId(booking.Id, booking.UserId, booking.MentorId, regenerate: true);
                var newMeetingUrl = await GenerateExternalMeetingUrl(newMeetingId, booking);

                // Update booking with new meeting URL
                booking.MeetingUrl = newMeetingUrl;
                await _bookingRepository.UpdateAsync(booking);

                result.IsSuccess = true;
                result.MeetingId = newMeetingId;
                result.MeetingUrl = newMeetingUrl;
                result.JoinUrl = GenerateJoinUrl(newMeetingId, booking.UserId);
                result.HostUrl = GenerateHostUrl(newMeetingId, booking.MentorId);
                result.IsRegenerated = true;
                result.PreviousMeetingUrl = booking.MeetingUrl;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error regenerating meeting URL: {ex.Message}";
            }

            return result;
        }

        public async Task<MeetingJoinResult> GetMeetingJoinInfoAsync(int bookingId, string userId)
        {
            var result = new MeetingJoinResult();

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Booking not found";
                    return result;
                }

                // Check authorization
                if (booking.UserId != userId && booking.MentorId != userId)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "You are not authorized to join this meeting";
                    return result;
                }

                // Check if meeting can be joined (within 15 minutes of start time)
                var timeUntilSession = booking.SessionStartTime - DateTime.UtcNow;
                if (timeUntilSession.TotalMinutes > 15)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting can only be joined within 15 minutes of start time";
                    return result;
                }

                // Check if meeting has already ended
                if (booking.SessionEndTime < DateTime.UtcNow)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting has already ended";
                    return result;
                }

                // Generate or retrieve meeting URL
                if (string.IsNullOrEmpty(booking.MeetingUrl))
                {
                    var urlResult = await GenerateMeetingUrlAsync(booking);
                    if (!urlResult.IsSuccess)
                    {
                        result.IsSuccess = false;
                        result.ErrorMessage = urlResult.ErrorMessage;
                        return result;
                    }
                    booking.MeetingUrl = urlResult.MeetingUrl;
                }

                var meetingId = ExtractMeetingIdFromUrl(booking.MeetingUrl);
                var isHost = booking.MentorId == userId;
                var participantName = await GetParticipantName(userId);

                result.IsSuccess = true;
                result.MeetingId = meetingId;
                result.JoinUrl = GenerateParticipantJoinUrl(meetingId, userId, isHost, participantName);
                result.IsHost = isHost;
                result.ParticipantName = participantName;
                result.SessionStartTime = booking.SessionStartTime;
                result.SessionEndTime = booking.SessionEndTime;
                result.CanJoinNow = timeUntilSession.TotalMinutes <= 15 && timeUntilSession.TotalMinutes > -30;
                result.MeetingStatus = GetMeetingStatus(booking);
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error getting meeting join info: {ex.Message}";
            }

            return result;
        }

        public async Task<MeetingStatusResult> GetMeetingStatusAsync(string meetingId)
        {
            var result = new MeetingStatusResult();

            try
            {
                // In a real implementation, this would call the external meeting service API
                // For now, we'll simulate the status based on booking data
                var booking = await FindBookingByMeetingId(meetingId);

                if (booking == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting not found";
                    return result;
                }

                result.IsSuccess = true;
                result.MeetingId = meetingId;
                result.Status = GetMeetingStatus(booking);
                result.StartTime = booking.SessionStartTime;
                result.EndTime = booking.SessionEndTime;
                result.DurationMinutes = (int)(booking.SessionEndTime - booking.SessionStartTime).TotalMinutes;
                result.ParticipantCount = await GetParticipantCount(meetingId);
                result.IsRecording = false; // Would be determined from external API
                result.HasWaitingRoom = true;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error getting meeting status: {ex.Message}";
            }

            return result;
        }

        public async Task<List<MeetingParticipantResult>> GetMeetingParticipantsAsync(string meetingId)
        {
            var participants = new List<MeetingParticipantResult>();

            try
            {
                var booking = await FindBookingByMeetingId(meetingId);
                if (booking == null)
                    return participants;

                // Add mentor as participant
                var mentor = await _userRepository.GetByIdAsync(booking.MentorId);
                if (mentor != null)
                {
                    participants.Add(new MeetingParticipantResult
                    {
                        UserId = mentor.Id,
                        Name = mentor.FirstName + " " + mentor.LastName,
                        Email = mentor.Email,
                        Role = "Host",
                        IsHost = true,
                        JoinedAt = booking.SessionStartTime.AddMinutes(-5), // Host joins 5 minutes early
                        Status = "Connected"
                    });
                }

                // Add user/mentee as participant
                var user = await _userRepository.GetByIdAsync(booking.UserId);
                if (user != null)
                {
                    participants.Add(new MeetingParticipantResult
                    {
                        UserId = user.Id,
                        Name = user.FirstName + " " + user.LastName,
                        Email = user.Email,
                        Role = "Participant",
                        IsHost = false,
                        JoinedAt = booking.SessionStartTime, // Participant joins at start time
                        Status = DateTime.UtcNow >= booking.SessionStartTime ? "Connected" : "Waiting"
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but return empty list
                Console.WriteLine($"Error getting meeting participants: {ex.Message}");
            }

            return participants;
        }

        public async Task<MeetingRecordingResult> StartMeetingRecordingAsync(string meetingId)
        {
            var result = new MeetingRecordingResult();

            try
            {
                var booking = await FindBookingByMeetingId(meetingId);
                if (booking == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting not found";
                    return result;
                }

                // Check if meeting is currently active
                var now = DateTime.UtcNow;
                if (now < booking.SessionStartTime || now > booking.SessionEndTime)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting is not currently active";
                    return result;
                }

                // In a real implementation, this would call the external meeting service API
                var recordingId = GenerateRecordingId(meetingId);

                result.IsSuccess = true;
                result.RecordingId = recordingId;
                result.MeetingId = meetingId;
                result.StartTime = now;
                result.Status = "Recording";
                result.RecordingUrl = $"https://recordings.example.com/{recordingId}";
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error starting meeting recording: {ex.Message}";
            }

            return result;
        }

        public async Task<MeetingRecordingResult> StopMeetingRecordingAsync(string meetingId)
        {
            var result = new MeetingRecordingResult();

            try
            {
                var booking = await FindBookingByMeetingId(meetingId);
                if (booking == null)
                {
                    result.IsSuccess = false;
                    result.ErrorMessage = "Meeting not found";
                    return result;
                }

                // In a real implementation, this would call the external meeting service API
                var recordingId = GenerateRecordingId(meetingId);

                result.IsSuccess = true;
                result.RecordingId = recordingId;
                result.MeetingId = meetingId;
                result.EndTime = DateTime.UtcNow;
                result.Status = "Completed";
                result.RecordingUrl = $"https://recordings.example.com/{recordingId}";
                result.DurationMinutes = (int)(DateTime.UtcNow - booking.SessionStartTime).TotalMinutes;
            }
            catch (Exception ex)
            {
                result.IsSuccess = false;
                result.ErrorMessage = $"Error stopping meeting recording: {ex.Message}";
            }

            return result;
        }

        public async Task<bool> EndMeetingAsync(string meetingId, string userId)
        {
            try
            {
                var booking = await FindBookingByMeetingId(meetingId);
                if (booking == null)
                    return false;

                // Check if user is the host (mentor)
                if (booking.MentorId != userId)
                    return false;

                // In a real implementation, this would call the external meeting service API
                // For now, we'll simulate ending the meeting by updating the booking status if needed

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<MeetingSessionResult>> GetUpcomingMeetingsAsync(string userId)
        {
            var meetings = new List<MeetingSessionResult>();

            try
            {
                var userBookings = await _bookingRepository.GetByUserIdAsync(userId);
                var upcomingBookings = userBookings
                    .Where(b => b.Status == BookingStatus.Confirmed && b.SessionStartTime > DateTime.UtcNow)
                    .OrderBy(b => b.SessionStartTime)
                    .Take(10)
                    .ToList();

                foreach (var booking in upcomingBookings)
                {
                    var mentor = await _userRepository.GetByIdAsync(booking.MentorId);
                    meetings.Add(new MeetingSessionResult
                    {
                        BookingId = booking.Id,
                        MeetingId = ExtractMeetingIdFromUrl(booking.MeetingUrl),
                        Title = $"Session with {mentor?.FirstName ?? "Mentor"}",
                        StartTime = booking.SessionStartTime,
                        EndTime = booking.SessionEndTime,
                        DurationMinutes = (int)(booking.SessionEndTime - booking.SessionStartTime).TotalMinutes,
                        MeetingUrl = booking.MeetingUrl,
                        Status = GetMeetingStatus(booking),
                        CanJoin = booking.SessionStartTime <= DateTime.UtcNow.AddMinutes(15) && booking.SessionEndTime > DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting upcoming meetings: {ex.Message}");
            }

            return meetings;
        }

        public async Task<MeetingAnalyticsResult> GetMeetingAnalyticsAsync(string mentorId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var result = new MeetingAnalyticsResult();

            try
            {
                var mentorBookings = await _bookingRepository.GetByMentorIdAsync(mentorId);
                var filteredBookings = mentorBookings
                    .Where(b => b.Status == BookingStatus.Completed)
                    .ToList();

                if (startDate.HasValue)
                    filteredBookings = filteredBookings.Where(b => b.SessionStartTime >= startDate.Value).ToList();

                if (endDate.HasValue)
                    filteredBookings = filteredBookings.Where(b => b.SessionStartTime <= endDate.Value).ToList();

                result.TotalMeetings = filteredBookings.Count;
                result.TotalMinutes = filteredBookings.Sum(b => (int)(b.SessionEndTime - b.SessionStartTime).TotalMinutes);
                result.TotalHours = Math.Round(result.TotalMinutes / 60.0, 2);
                result.AverageMeetingDuration = result.TotalMeetings > 0 ? Math.Round((double)result.TotalMinutes / result.TotalMeetings, 2) : 0;
                result.NoShowRate = CalculateNoShowRate(mentorBookings);
                result.CancellationRate = CalculateCancellationRate(mentorBookings);

                // Group by month for trends
                result.MonthlyStats = filteredBookings
                    .GroupBy(b => new { b.SessionStartTime.Year, b.SessionStartTime.Month })
                    .Select(g => new MonthlyMeetingStats
                    {
                        Year = g.Key.Year,
                        Month = g.Key.Month,
                        MeetingCount = g.Count(),
                        TotalMinutes = g.Sum(b => (int)(b.SessionEndTime - b.SessionStartTime).TotalMinutes)
                    })
                    .OrderBy(x => x.Year)
                    .ThenBy(x => x.Month)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting meeting analytics: {ex.Message}");
            }

            return result;
        }

        public async Task<MeetingSettingsResult> GetMeetingSettingsAsync()
        {
            // In a real implementation, this would be retrieved from database or configuration
            return new MeetingSettingsResult
            {
                DefaultDurationMinutes = 60,
                MaxDurationMinutes = 480,
                BufferTimeMinutes = 15,
                EnableRecording = true,
                EnableWaitingRoom = true,
                EnableBreakoutRooms = false,
                DefaultMeetingPlatform = "Zoom", // Zoom, Teams, Google Meet
                AutoGenerateUrls = true,
                UrlValidityHours = 24,
                EnableMeetingReminders = true,
                ReminderAdvanceMinutes = 60
            };
        }

        public async Task<MeetingSettingsResult> UpdateMeetingSettingsAsync(MeetingSettingsDto settings)
        {
            // In a real implementation, this would update the database/configuration
            return new MeetingSettingsResult
            {
                DefaultDurationMinutes = settings.DefaultDurationMinutes,
                MaxDurationMinutes = settings.MaxDurationMinutes,
                BufferTimeMinutes = settings.BufferTimeMinutes,
                EnableRecording = settings.EnableRecording,
                EnableWaitingRoom = settings.EnableWaitingRoom,
                EnableBreakoutRooms = settings.EnableBreakoutRooms,
                DefaultMeetingPlatform = settings.DefaultMeetingPlatform,
                AutoGenerateUrls = settings.AutoGenerateUrls,
                UrlValidityHours = settings.UrlValidityHours,
                EnableMeetingReminders = settings.EnableMeetingReminders,
                ReminderAdvanceMinutes = settings.ReminderAdvanceMinutes
            };
        }

        // Private helper methods
        private string GenerateMeetingId(int bookingId, string userId, string mentorId, bool regenerate = false)
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var randomSuffix = new Random().Next(1000, 9999);
            var regenerateFlag = regenerate ? "R" : "N";
            return $"MENT-{bookingId}-{userId.Substring(0, 8)}-{mentorId.Substring(0, 8)}-{timestamp}-{randomSuffix}-{regenerateFlag}";
        }

        private async Task<string> GenerateExternalMeetingUrl(string meetingId, Booking booking)
        {
            // In a real implementation, this would integrate with Zoom, Microsoft Teams, or Google Meet APIs
            // For now, we'll generate a mock URL
            await Task.Delay(100); // Simulate API call

            return $"https://meeting.example.com/join/{meetingId}";
        }

        private string GenerateJoinUrl(string meetingId, string userId)
        {
            return $"https://meeting.example.com/join/{meetingId}?participant={userId}";
        }

        private string GenerateHostUrl(string meetingId, string mentorId)
        {
            return $"https://meeting.example.com/host/{meetingId}?host={mentorId}";
        }

        private string GenerateParticipantJoinUrl(string meetingId, string userId, bool isHost, string participantName)
        {
            var role = isHost ? "host" : "participant";
            return $"https://meeting.example.com/join/{meetingId}?user={userId}&role={role}&name={Uri.EscapeDataString(participantName)}";
        }

        private string ExtractMeetingIdFromUrl(string? meetingUrl)
        {
            if (string.IsNullOrEmpty(meetingUrl))
                return string.Empty;

            var parts = meetingUrl.Split('/');
            return parts.LastOrDefault()?.Split('?').FirstOrDefault() ?? string.Empty;
        }

        private async Task<string> GetParticipantName(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? $"{user.FirstName} {user.LastName}" : "Unknown User";
        }

        private string GetMeetingStatus(Booking booking)
        {
            var now = DateTime.UtcNow;

            if (booking.Status == BookingStatus.Cancelled)
                return "Cancelled";

            if (booking.Status == BookingStatus.Completed)
                return "Completed";

            if (now < booking.SessionStartTime.AddMinutes(-15))
                return "Scheduled";

            if (now >= booking.SessionStartTime.AddMinutes(-15) && now <= booking.SessionEndTime)
                return "Active";

            if (now > booking.SessionEndTime)
                return "Ended";

            return "Scheduled";
        }

        private async Task<Booking?> FindBookingByMeetingId(string meetingId)
        {
            // In a real implementation, this would search for bookings with the given meeting ID
            // For now, we'll return null as this would require additional database schema
            return null;
        }

        private async Task<int> GetParticipantCount(string meetingId)
        {
            // In a real implementation, this would call the external meeting service API
            await Task.Delay(50);
            return new Random().Next(1, 5);
        }

        private string GenerateRecordingId(string meetingId)
        {
            return $"REC-{meetingId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        private double CalculateNoShowRate(IEnumerable<Booking> bookings)
        {
            var totalBookings = bookings.Count(b => b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Completed || b.Status == BookingStatus.NoShow);
            var noShows = bookings.Count(b => b.Status == BookingStatus.NoShow);

            return totalBookings > 0 ? Math.Round((double)noShows / totalBookings * 100, 2) : 0;
        }

        private double CalculateCancellationRate(IEnumerable<Booking> bookings)
        {
            var totalBookings = bookings.Count();
            var cancellations = bookings.Count(b => b.Status == BookingStatus.Cancelled);

            return totalBookings > 0 ? Math.Round((double)cancellations / totalBookings * 100, 2) : 0;
        }
    }

    // Result classes
    public class MeetingUrlResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MeetingId { get; set; }
        public string? MeetingUrl { get; set; }
        public string? JoinUrl { get; set; }
        public string? HostUrl { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public bool IsRegenerated { get; set; }
        public string? PreviousMeetingUrl { get; set; }
    }

    public class MeetingJoinResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MeetingId { get; set; }
        public string? JoinUrl { get; set; }
        public bool IsHost { get; set; }
        public string? ParticipantName { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public bool CanJoinNow { get; set; }
        public string MeetingStatus { get; set; } = string.Empty;
    }

    public class MeetingStatusResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? MeetingId { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public int ParticipantCount { get; set; }
        public bool IsRecording { get; set; }
        public bool HasWaitingRoom { get; set; }
    }

    public class MeetingParticipantResult
    {
        public string UserId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsHost { get; set; }
        public DateTime JoinedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class MeetingRecordingResult
    {
        public bool IsSuccess { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RecordingId { get; set; }
        public string? MeetingId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? RecordingUrl { get; set; }
        public int DurationMinutes { get; set; }
    }

    public class MeetingSessionResult
    {
        public int BookingId { get; set; }
        public string? MeetingId { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public int DurationMinutes { get; set; }
        public string? MeetingUrl { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool CanJoin { get; set; }
    }

    public class MeetingAnalyticsResult
    {
        public int TotalMeetings { get; set; }
        public int TotalMinutes { get; set; }
        public double TotalHours { get; set; }
        public double AverageMeetingDuration { get; set; }
        public double NoShowRate { get; set; }
        public double CancellationRate { get; set; }
        public List<MonthlyMeetingStats> MonthlyStats { get; set; } = new List<MonthlyMeetingStats>();
    }

    public class MonthlyMeetingStats
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int MeetingCount { get; set; }
        public int TotalMinutes { get; set; }
    }

    public class MeetingSettingsResult
    {
        public int DefaultDurationMinutes { get; set; }
        public int MaxDurationMinutes { get; set; }
        public int BufferTimeMinutes { get; set; }
        public bool EnableRecording { get; set; }
        public bool EnableWaitingRoom { get; set; }
        public bool EnableBreakoutRooms { get; set; }
        public string DefaultMeetingPlatform { get; set; } = string.Empty;
        public bool AutoGenerateUrls { get; set; }
        public int UrlValidityHours { get; set; }
        public bool EnableMeetingReminders { get; set; }
        public int ReminderAdvanceMinutes { get; set; }
    }

    // DTO classes
    public class MeetingSettingsDto
    {
        public int DefaultDurationMinutes { get; set; } = 60;
        public int MaxDurationMinutes { get; set; } = 480;
        public int BufferTimeMinutes { get; set; } = 15;
        public bool EnableRecording { get; set; } = true;
        public bool EnableWaitingRoom { get; set; } = true;
        public bool EnableBreakoutRooms { get; set; } = false;
        public string DefaultMeetingPlatform { get; set; } = "Zoom";
        public bool AutoGenerateUrls { get; set; } = true;
        public int UrlValidityHours { get; set; } = 24;
        public bool EnableMeetingReminders { get; set; } = true;
        public int ReminderAdvanceMinutes { get; set; } = 60;
    }
}
