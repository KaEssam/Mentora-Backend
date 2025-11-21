using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Data;

public class ReminderRepository : IReminderRepository
{
    private readonly ApplicationDbContext _context;

    public ReminderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Reminder?> GetByIdAsync(int id)
    {
        return await _context.Reminders

            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reminder>> GetByUserIdAsync(string userId)
    {
        return await _context.Reminders
            .Where(r => r.UserId == userId)

            .OrderByDescending(r => r.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetBySessionIdAsync(int sessionId)
    {
        return await _context.Reminders
            .Where(r => r.SessionId == sessionId)
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetScheduledRemindersAsync(DateTime scheduledBefore)
    {
        return await _context.Reminders
            .Where(r => r.Status == ReminderStatus.Scheduled &&
                       r.ScheduledAt <= scheduledBefore &&
                       r.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(r => r.ScheduledAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetPendingRetriesAsync()
    {
        return await _context.Reminders
            .Where(r => r.Status == ReminderStatus.Failed &&
                       r.CanRetry &&
                       r.NextRetryAt <= DateTime.UtcNow)
            .OrderBy(r => r.NextRetryAt)
            .ToListAsync();
    }

    public async Task<Reminder> CreateAsync(Reminder reminder)
    {
        _context.Reminders.Add(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<Reminder> UpdateAsync(Reminder reminder)
    {
        _context.Reminders.Update(reminder);
        await _context.SaveChangesAsync();
        return reminder;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var reminder = await _context.Reminders.FindAsync(id);
        if (reminder == null) return false;

        _context.Reminders.Remove(reminder);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Reminders.AnyAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reminder>> GetByStatusAsync(ReminderStatus status)
    {
        return await _context.Reminders
            .Where(r => r.Status == status)

            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetByTypeAsync(ReminderType type)
    {
        return await _context.Reminders
            .Where(r => r.Type == type)

            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reminder>> GetOverdueRemindersAsync()
    {
        return await _context.Reminders
            .Where(r => r.IsOverdue)

            .OrderBy(r => r.ScheduledAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByStatusAsync(ReminderStatus status)
    {
        return await _context.Reminders.CountAsync(r => r.Status == status);
    }

    public async Task<int> GetCountByTypeAsync(ReminderType type)
    {
        return await _context.Reminders.CountAsync(r => r.Type == type);
    }
}

public class ReminderSettingsRepository : IReminderSettingsRepository
{
    private readonly ApplicationDbContext _context;

    public ReminderSettingsRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReminderSettings?> GetByUserIdAsync(string userId)
    {
        return await _context.ReminderSettings
            .FirstOrDefaultAsync(rs => rs.UserId == userId);
    }

    public async Task<ReminderSettings> CreateAsync(ReminderSettings settings)
    {
        _context.ReminderSettings.Add(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<ReminderSettings> UpdateAsync(ReminderSettings settings)
    {
        _context.ReminderSettings.Update(settings);
        await _context.SaveChangesAsync();
        return settings;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var settings = await _context.ReminderSettings.FindAsync(id);
        if (settings == null) return false;

        _context.ReminderSettings.Remove(settings);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.ReminderSettings.AnyAsync(rs => rs.Id == id);
    }
}
