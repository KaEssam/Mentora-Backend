using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mentora.Core.Data;

public class ReminderSettings
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    // Reminder timing settings (in minutes/hours before session)
    public int DefaultReminderMinutesBefore { get; set; } = 60; // 1 hour before
    public int SecondReminderMinutesBefore { get; set; } = 15; // 15 minutes before
    public int FollowUpHoursAfter { get; set; } = 24; // 24 hours after session

    // Email preferences
    public bool EnableSessionReminders { get; set; } = true;
    public bool EnableSessionConfirmations { get; set; } = true;
    public bool EnableFollowUpReminders { get; set; } = true;
    public bool EnableFeedbackRequests { get; set; } = true;

    // Communication channels
    public bool EmailNotificationsEnabled { get; set; } = true;
    public bool SmsNotificationsEnabled { get; set; } = false;
    public bool PushNotificationsEnabled { get; set; } = false;

    // Notification frequency
    public int MaxRemindersPerSession { get; set; } = 3;
    public bool ConsolidateReminders { get; set; } = false; // Combine multiple reminders into one email

    // Time zone handling
    public string UserTimeZone { get; set; } = "UTC";

    // Quiet hours
    public bool RespectQuietHours { get; set; } = true;
    public TimeOnly QuietHoursStart { get; set; } = new TimeOnly(22, 0); // 10 PM
    public TimeOnly QuietHoursEnd { get; set; } = new TimeOnly(8, 0); // 8 AM

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Note: Navigation property configured in Infrastructure layer

    // Helper methods
    public bool ShouldSendReminder(DateTime sessionStart, ReminderType reminderType)
    {
        if (!EnableSessionReminders && reminderType == ReminderType.SessionReminder)
            return false;

        if (!EnableSessionConfirmations && reminderType == ReminderType.SessionConfirmation)
            return false;

        if (!EnableFollowUpReminders && reminderType == ReminderType.FollowUp)
            return false;

        if (!EnableFeedbackRequests && reminderType == ReminderType.FeedbackRequest)
            return false;

        // Check quiet hours
        if (RespectQuietHours && IsDuringQuietHours())
            return false;

        return true;
    }

    private bool IsDuringQuietHours()
    {
        var now = TimeOnly.FromDateTime(DateTime.UtcNow);
        return now >= QuietHoursStart || now <= QuietHoursEnd;
    }

    public DateTime AdjustForQuietHours(DateTime scheduledTime)
    {
        if (!RespectQuietHours || !IsDuringQuietHours())
            return scheduledTime;

        var tomorrow = scheduledTime.Date.AddDays(1);
        return new DateTime(tomorrow.Year, tomorrow.Month, tomorrow.Day,
                           QuietHoursEnd.Hour, QuietHoursEnd.Minute, 0);
    }
}
