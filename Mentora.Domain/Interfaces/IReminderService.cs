using Mentora.Core.Data;
using Mentora.Domain.DTOs;

namespace Mentora.Domain.Interfaces;

public interface IReminderService
{
    // Reminder management
    Task<ResponseReminderDto> CreateReminderAsync(CreateReminderDto createDto, string userId);
    Task<ResponseReminderDto?> GetReminderByIdAsync(int id, string userId);
    Task<IEnumerable<ResponseReminderDto>> GetUserRemindersAsync(string userId);
    Task<IEnumerable<ResponseReminderDto>> GetSessionRemindersAsync(int sessionId, string userId);
    Task<ResponseReminderDto?> UpdateReminderAsync(int id, UpdateReminderDto updateDto, string userId);
    Task<bool> DeleteReminderAsync(int id, string userId);

    // Reminder scheduling
    Task<IEnumerable<ResponseReminderDto>> ScheduleSessionRemindersAsync(int sessionId);
    Task<IEnumerable<ResponseReminderDto>> ScheduleBulkRemindersAsync(BulkScheduleRemindersDto bulkDto, string userId);
    Task<bool> CancelReminderAsync(int id, string userId);
    Task<bool> RescheduleReminderAsync(int id, DateTime newScheduledAt, string userId);

    // Reminder processing
    Task ProcessScheduledRemindersAsync();
    Task ProcessRetriesAsync();
    Task<bool> SendReminderAsync(int reminderId);
    Task<ReminderStatsDto> GetReminderStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);

    // Settings management
    Task<ReminderSettingsDto> GetReminderSettingsAsync(string userId);
    Task<ReminderSettingsDto> UpdateReminderSettingsAsync(UpdateReminderSettingsDto updateDto, string userId);
    Task<ReminderSettingsDto> CreateDefaultSettingsAsync(string userId);
}

public interface IEmailService
{
    Task<bool> SendEmailAsync(string to, string subject, string message);
    Task<bool> SendSessionReminderEmailAsync(string to, string userName, Session session);
    Task<bool> SendSessionConfirmationEmailAsync(string to, string userName, Session session);
    Task<bool> SendFollowUpEmailAsync(string to, string userName, Session session);
    Task<bool> SendFeedbackRequestEmailAsync(string to, string userName, Session session);
}
