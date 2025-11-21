using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IReminderRepository
{
    Task<Reminder?> GetByIdAsync(int id);
    Task<IEnumerable<Reminder>> GetByUserIdAsync(string userId);
    Task<IEnumerable<Reminder>> GetBySessionIdAsync(int sessionId);
    Task<IEnumerable<Reminder>> GetScheduledRemindersAsync(DateTime scheduledBefore);
    Task<IEnumerable<Reminder>> GetPendingRetriesAsync();
    Task<Reminder> CreateAsync(Reminder reminder);
    Task<Reminder> UpdateAsync(Reminder reminder);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<IEnumerable<Reminder>> GetByStatusAsync(ReminderStatus status);
    Task<IEnumerable<Reminder>> GetByTypeAsync(ReminderType type);
    Task<IEnumerable<Reminder>> GetOverdueRemindersAsync();
    Task<int> GetCountByStatusAsync(ReminderStatus status);
    Task<int> GetCountByTypeAsync(ReminderType type);
}

public interface IReminderSettingsRepository
{
    Task<ReminderSettings?> GetByUserIdAsync(string userId);
    Task<ReminderSettings> CreateAsync(ReminderSettings settings);
    Task<ReminderSettings> UpdateAsync(ReminderSettings settings);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
