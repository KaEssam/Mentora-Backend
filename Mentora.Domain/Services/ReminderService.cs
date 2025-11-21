using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using AutoMapper;

namespace Mentora.Domain.Services;

public class ReminderService : IReminderService
{
    private readonly IReminderRepository _reminderRepository;
    private readonly IReminderSettingsRepository _settingsRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IEmailService _emailService;
    private readonly IMapper _mapper;

    public ReminderService(
        IReminderRepository reminderRepository,
        IReminderSettingsRepository settingsRepository,
        ISessionRepository sessionRepository,
        IEmailService emailService,
        IMapper mapper)
    {
        _reminderRepository = reminderRepository;
        _settingsRepository = settingsRepository;
        _sessionRepository = sessionRepository;
        _emailService = emailService;
        _mapper = mapper;
    }

    public async Task<ResponseReminderDto> CreateReminderAsync(CreateReminderDto createDto, string userId)
    {
        // Validate session exists
        var session = await _sessionRepository.GetByIdAsync(createDto.SessionId);
        if (session == null)
            throw new ArgumentException($"Session with ID {createDto.SessionId} not found.");

        // Check user permissions for the session
        if (session.MentorId != userId && !await IsUserParticipantAsync(session, userId))
            throw new UnauthorizedAccessException("User is not authorized to create reminders for this session.");

        var reminder = _mapper.Map<Reminder>(createDto);
        reminder.UserId = userId;
        reminder.Status = ReminderStatus.Scheduled;

        var createdReminder = await _reminderRepository.CreateAsync(reminder);

        return _mapper.Map<ResponseReminderDto>(createdReminder);
    }

    public async Task<ResponseReminderDto?> GetReminderByIdAsync(int id, string userId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null || reminder.UserId != userId)
            return null;

        return _mapper.Map<ResponseReminderDto>(reminder);
    }

    public async Task<IEnumerable<ResponseReminderDto>> GetUserRemindersAsync(string userId)
    {
        var reminders = await _reminderRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<ResponseReminderDto>>(reminders);
    }

    public async Task<IEnumerable<ResponseReminderDto>> GetSessionRemindersAsync(int sessionId, string userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new ArgumentException($"Session with ID {sessionId} not found.");

        if (session.MentorId != userId && !await IsUserParticipantAsync(session, userId))
            throw new UnauthorizedAccessException("User is not authorized to view reminders for this session.");

        var reminders = await _reminderRepository.GetBySessionIdAsync(sessionId);
        return _mapper.Map<IEnumerable<ResponseReminderDto>>(reminders);
    }

    public async Task<ResponseReminderDto?> UpdateReminderAsync(int id, UpdateReminderDto updateDto, string userId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null || reminder.UserId != userId)
            return null;

        if (reminder.Status == ReminderStatus.Sent)
            throw new InvalidOperationException("Cannot update a reminder that has already been sent.");

        _mapper.Map(updateDto, reminder);
        reminder.UpdatedAt = DateTime.UtcNow;

        var updatedReminder = await _reminderRepository.UpdateAsync(reminder);
        return _mapper.Map<ResponseReminderDto>(updatedReminder);
    }

    public async Task<bool> DeleteReminderAsync(int id, string userId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null || reminder.UserId != userId)
            return false;

        if (reminder.Status == ReminderStatus.Sent)
            throw new InvalidOperationException("Cannot delete a reminder that has already been sent.");

        return await _reminderRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ResponseReminderDto>> ScheduleSessionRemindersAsync(int sessionId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
            throw new ArgumentException($"Session with ID {sessionId} not found.");

        var settings = await _settingsRepository.GetByUserIdAsync(session.MentorId);
        if (settings == null)
        {
            settings = await CreateDefaultSettingsInternalAsync(session.MentorId);
        }

        var reminders = new List<Reminder>();

        // Schedule session reminders
        if (settings.ShouldSendReminder(session.StartAt, ReminderType.SessionReminder))
        {
            reminders.Add(await CreateSessionReminderAsync(session, settings, ReminderType.SessionReminder,
                settings.DefaultReminderMinutesBefore));
        }

        // Schedule second reminder
        if (settings.SecondReminderMinutesBefore > 0 &&
            settings.ShouldSendReminder(session.StartAt, ReminderType.SessionReminder))
        {
            reminders.Add(await CreateSessionReminderAsync(session, settings, ReminderType.SessionReminder,
                settings.SecondReminderMinutesBefore));
        }

        // Schedule confirmation email
        if (settings.ShouldSendReminder(session.StartAt, ReminderType.SessionConfirmation))
        {
            reminders.Add(await CreateSessionReminderAsync(session, settings, ReminderType.SessionConfirmation, 0));
        }

        // Schedule follow-up
        if (settings.EnableFollowUpReminders)
        {
            var followUpTime = session.EndAt.AddHours(settings.FollowUpHoursAfter);
            reminders.Add(await CreateFollowUpReminderAsync(session, settings));
        }

        // Schedule feedback request
        if (settings.EnableFeedbackRequests)
        {
            var feedbackTime = session.EndAt.AddHours(2); // 2 hours after session
            reminders.Add(await CreateFeedbackReminderAsync(session, settings));
        }

        var createdReminders = new List<ResponseReminderDto>();
        foreach (var reminder in reminders)
        {
            var created = await _reminderRepository.CreateAsync(reminder);
            createdReminders.Add(_mapper.Map<ResponseReminderDto>(created));
        }

        return createdReminders;
    }

    public async Task<IEnumerable<ResponseReminderDto>> ScheduleBulkRemindersAsync(BulkScheduleRemindersDto bulkDto, string userId)
    {
        var results = new List<ResponseReminderDto>();

        foreach (var sessionId in bulkDto.SessionIds)
        {
            try
            {
                var session = await _sessionRepository.GetByIdAsync(sessionId);
                if (session == null || session.MentorId != userId)
                    continue;

                var reminder = new Reminder
                {
                    UserId = userId,
                    SessionId = sessionId,
                    Type = bulkDto.ReminderType,
                    ScheduledAt = bulkDto.ScheduleAt,
                    Subject = bulkDto.CustomSubject,
                    Message = bulkDto.CustomMessage,
                    Status = ReminderStatus.Scheduled
                };

                var created = await _reminderRepository.CreateAsync(reminder);
                results.Add(_mapper.Map<ResponseReminderDto>(created));
            }
            catch (Exception ex)
            {
                // Error scheduling reminder
            }
        }

        return results;
    }

    public async Task<bool> CancelReminderAsync(int id, string userId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null || reminder.UserId != userId)
            return false;

        if (reminder.Status == ReminderStatus.Sent)
            throw new InvalidOperationException("Cannot cancel a reminder that has already been sent.");

        reminder.Status = ReminderStatus.Cancelled;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _reminderRepository.UpdateAsync(reminder);
        return true;
    }

    public async Task<bool> RescheduleReminderAsync(int id, DateTime newScheduledAt, string userId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(id);
        if (reminder == null || reminder.UserId != userId)
            return false;

        if (reminder.Status == ReminderStatus.Sent)
            throw new InvalidOperationException("Cannot reschedule a reminder that has already been sent.");

        reminder.ScheduledAt = newScheduledAt;
        reminder.UpdatedAt = DateTime.UtcNow;

        await _reminderRepository.UpdateAsync(reminder);
        return true;
    }

    public async Task ProcessScheduledRemindersAsync()
    {
        var scheduledReminders = await _reminderRepository.GetScheduledRemindersAsync(DateTime.UtcNow);

        foreach (var reminder in scheduledReminders)
        {
            try
            {
                await SendReminderAsync(reminder.Id);
            }
            catch (Exception ex)
            {
                // Error processing reminder
            }
        }
    }

    public async Task ProcessRetriesAsync()
    {
        var pendingRetries = await _reminderRepository.GetPendingRetriesAsync();

        foreach (var reminder in pendingRetries)
        {
            try
            {
                await SendReminderAsync(reminder.Id);
            }
            catch (Exception ex)
            {
                // Error retrying reminder
            }
        }
    }

    public async Task<bool> SendReminderAsync(int reminderId)
    {
        var reminder = await _reminderRepository.GetByIdAsync(reminderId);
        if (reminder == null)
            return false;

        try
        {
            var session = await _sessionRepository.GetByIdAsync(reminder.SessionId);
            if (session == null)
            {
                reminder.MarkAsFailed("Associated session not found");
                await _reminderRepository.UpdateAsync(reminder);
                return false;
            }

            var success = reminder.Type switch
            {
                ReminderType.SessionReminder => await _emailService.SendSessionReminderEmailAsync(
                    reminder.RecipientEmail ?? GetUserEmail(reminder.UserId),
                    GetUserName(reminder.UserId),
                    session),
                ReminderType.SessionConfirmation => await _emailService.SendSessionConfirmationEmailAsync(
                    reminder.RecipientEmail ?? GetUserEmail(reminder.UserId),
                    GetUserName(reminder.UserId),
                    session),
                ReminderType.FollowUp => await _emailService.SendFollowUpEmailAsync(
                    reminder.RecipientEmail ?? GetUserEmail(reminder.UserId),
                    GetUserName(reminder.UserId),
                    session),
                ReminderType.FeedbackRequest => await _emailService.SendFeedbackRequestEmailAsync(
                    reminder.RecipientEmail ?? GetUserEmail(reminder.UserId),
                    GetUserName(reminder.UserId),
                    session),
                _ => await _emailService.SendEmailAsync(
                    reminder.RecipientEmail ?? GetUserEmail(reminder.UserId),
                    reminder.Subject ?? "Session Reminder",
                    reminder.Message ?? "This is a reminder for your upcoming session.")
            };

            if (success)
            {
                reminder.MarkAsSent();
                // Reminder sent successfully
            }
            else
            {
                reminder.MarkAsFailed("Email service failed to send reminder");
                // Email service failed to send reminder
            }

            await _reminderRepository.UpdateAsync(reminder);
            return success;
        }
        catch (Exception ex)
        {
            reminder.MarkAsFailed(ex.Message);
            await _reminderRepository.UpdateAsync(reminder);
            // Exception occurred while sending reminder
            return false;
        }
    }

    public async Task<ReminderStatsDto> GetReminderStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var allReminders = await _reminderRepository.GetByUserIdAsync(userId);

        var filteredReminders = allReminders.Where(r =>
            (!startDate.HasValue || r.CreatedAt >= startDate.Value) &&
            (!endDate.HasValue || r.CreatedAt <= endDate.Value)).ToList();

        var stats = new ReminderStatsDto
        {
            TotalReminders = filteredReminders.Count,
            ScheduledReminders = filteredReminders.Count(r => r.Status == ReminderStatus.Scheduled),
            SentReminders = filteredReminders.Count(r => r.Status == ReminderStatus.Sent),
            FailedReminders = filteredReminders.Count(r => r.Status == ReminderStatus.Failed),
            CancelledReminders = filteredReminders.Count(r => r.Status == ReminderStatus.Cancelled),
            PendingDelivery = filteredReminders.Count(r => r.Status == ReminderStatus.Scheduled && r.ScheduledAt <= DateTime.UtcNow)
        };

        stats.SuccessRate = stats.TotalReminders > 0 ?
            (double)stats.SentReminders / stats.TotalReminders * 100 : 0;

        stats.RemindersByType = filteredReminders
            .GroupBy(r => r.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        stats.MonthlyStats = filteredReminders
            .GroupBy(r => r.CreatedAt.ToString("yyyy-MM"))
            .Select(g => new MonthlyReminderStatsDto
            {
                Month = g.Key,
                TotalSent = g.Count(),
                SuccessfulDeliveries = g.Count(r => r.Status == ReminderStatus.Sent),
                FailedDeliveries = g.Count(r => r.Status == ReminderStatus.Failed)
            })
            .OrderBy(m => m.Month)
            .ToList();

        foreach (var monthly in stats.MonthlyStats)
        {
            monthly.SuccessRate = monthly.TotalSent > 0 ?
                (double)monthly.SuccessfulDeliveries / monthly.TotalSent * 100 : 0;
        }

        return stats;
    }

    public async Task<ReminderSettingsDto> GetReminderSettingsAsync(string userId)
    {
        var settings = await _settingsRepository.GetByUserIdAsync(userId);
        return settings != null ? _mapper.Map<ReminderSettingsDto>(settings) : await CreateDefaultSettingsAsync(userId);
    }

    public async Task<ReminderSettingsDto> UpdateReminderSettingsAsync(UpdateReminderSettingsDto updateDto, string userId)
    {
        var settings = await _settingsRepository.GetByUserIdAsync(userId);
        if (settings == null)
        {
            settings = await CreateDefaultSettingsInternalAsync(userId);
        }

        _mapper.Map(updateDto, settings);
        settings.UpdatedAt = DateTime.UtcNow;

        var updatedSettings = await _settingsRepository.UpdateAsync(settings);
        return _mapper.Map<ReminderSettingsDto>(updatedSettings);
    }

    public async Task<ReminderSettingsDto> CreateDefaultSettingsAsync(string userId)
    {
        var defaultSettings = await CreateDefaultSettingsInternalAsync(userId);
        return _mapper.Map<ReminderSettingsDto>(defaultSettings);
    }

    private async Task<ReminderSettings> CreateDefaultSettingsInternalAsync(string userId)
    {
        var settings = new ReminderSettings
        {
            UserId = userId,
            DefaultReminderMinutesBefore = 60,
            SecondReminderMinutesBefore = 15,
            FollowUpHoursAfter = 24,
            EnableSessionReminders = true,
            EnableSessionConfirmations = true,
            EnableFollowUpReminders = true,
            EnableFeedbackRequests = true,
            EmailNotificationsEnabled = true,
            SmsNotificationsEnabled = false,
            PushNotificationsEnabled = false,
            MaxRemindersPerSession = 3,
            ConsolidateReminders = false,
            UserTimeZone = "UTC",
            RespectQuietHours = true,
            QuietHoursStart = new TimeOnly(22, 0),
            QuietHoursEnd = new TimeOnly(8, 0)
        };

        return await _settingsRepository.CreateAsync(settings);
    }

    private async Task<Reminder> CreateSessionReminderAsync(Session session, ReminderSettings settings, ReminderType type, int minutesBefore)
    {
        var scheduledTime = session.StartAt.AddMinutes(-minutesBefore);
        scheduledTime = settings.AdjustForQuietHours(scheduledTime);

        return new Reminder
        {
            UserId = session.MentorId,
            SessionId = session.Id,
            Type = type,
            ScheduledAt = scheduledTime,
            Subject = GetDefaultSubject(type),
            Message = GetDefaultMessage(type, session),
            Status = ReminderStatus.Scheduled
        };
    }

    private async Task<Reminder> CreateFollowUpReminderAsync(Session session, ReminderSettings settings)
    {
        var scheduledTime = session.EndAt.AddHours(settings.FollowUpHoursAfter);
        scheduledTime = settings.AdjustForQuietHours(scheduledTime);

        return new Reminder
        {
            UserId = session.MentorId,
            SessionId = session.Id,
            Type = ReminderType.FollowUp,
            ScheduledAt = scheduledTime,
            Subject = "Session Follow-up",
            Message = $"How did your {session.Type.ToString()} session go? We'd love to hear your feedback.",
            Status = ReminderStatus.Scheduled
        };
    }

    private async Task<Reminder> CreateFeedbackReminderAsync(Session session, ReminderSettings settings)
    {
        var scheduledTime = session.EndAt.AddHours(2);
        scheduledTime = settings.AdjustForQuietHours(scheduledTime);

        return new Reminder
        {
            UserId = session.MentorId,
            SessionId = session.Id,
            Type = ReminderType.FeedbackRequest,
            ScheduledAt = scheduledTime,
            Subject = "Feedback Request",
            Message = $"Please share your feedback about your {session.Type.ToString()} session with your mentee.",
            Status = ReminderStatus.Scheduled
        };
    }

    private async Task<bool> IsUserParticipantAsync(Session session, string userId)
    {
        // This would typically check if the user is a participant in the session
        // For now, we'll assume only the mentor can manage reminders
        return false;
    }

    private string GetDefaultSubject(ReminderType type)
    {
        return type switch
        {
            ReminderType.SessionReminder => "Upcoming Session Reminder",
            ReminderType.SessionConfirmation => "Session Confirmation",
            ReminderType.FollowUp => "Session Follow-up",
            ReminderType.FeedbackRequest => "Feedback Request",
            _ => "Reminder"
        };
    }

    private string GetDefaultMessage(ReminderType type, Session session)
    {
        return type switch
        {
            ReminderType.SessionReminder => $"You have an upcoming {session.Type.ToString()} session scheduled for {session.StartAt:yyyy-MM-dd HH:mm}.",
            ReminderType.SessionConfirmation => $"Your {session.Type.ToString()} session has been confirmed for {session.StartAt:yyyy-MM-dd HH:mm}.",
            ReminderType.FollowUp => $"Following up on your {session.Type.ToString()} session from {session.StartAt:yyyy-MM-dd HH:mm}.",
            ReminderType.FeedbackRequest => $"Please provide feedback for your {session.Type.ToString()} session.",
            _ => $"{session.Type.ToString()} session reminder."
        };
    }

    private string GetUserEmail(string userId)
    {
        // This would typically fetch from user service or database
        // For now, return a placeholder
        return $"user_{userId}@example.com";
    }

    private string GetUserName(string userId)
    {
        // This would typically fetch from user service or database
        // For now, return a placeholder
        return $"User {userId}";
    }
}
