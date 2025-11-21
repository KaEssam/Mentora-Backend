using System.ComponentModel.DataAnnotations;
using Mentora.Core.Data;

namespace Mentora.Domain.DTOs;

public class CreateReminderDto
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    public ReminderType Type { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    [StringLength(500)]
    public string? Subject { get; set; }

    [StringLength(2000)]
    public string? Message { get; set; }

    [StringLength(100)]
    public string? RecipientEmail { get; set; }
}

public class UpdateReminderDto
{
    public ReminderType? Type { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public ReminderStatus? Status { get; set; }

    [StringLength(500)]
    public string? Subject { get; set; }

    [StringLength(2000)]
    public string? Message { get; set; }

    [StringLength(100)]
    public string? RecipientEmail { get; set; }
}

public class ResponseReminderDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public ReminderType Type { get; set; }
    public DateTime ScheduledAt { get; set; }
    public DateTime? SentAt { get; set; }
    public ReminderStatus Status { get; set; }
    public string? Subject { get; set; }
    public string? Message { get; set; }
    public string? RecipientEmail { get; set; }
    public int DeliveryAttempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class ReminderSettingsDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int DefaultReminderMinutesBefore { get; set; }
    public int SecondReminderMinutesBefore { get; set; }
    public int FollowUpHoursAfter { get; set; }
    public bool EnableSessionReminders { get; set; }
    public bool EnableSessionConfirmations { get; set; }
    public bool EnableFollowUpReminders { get; set; }
    public bool EnableFeedbackRequests { get; set; }
    public bool EmailNotificationsEnabled { get; set; }
    public bool SmsNotificationsEnabled { get; set; }
    public bool PushNotificationsEnabled { get; set; }
    public int MaxRemindersPerSession { get; set; }
    public bool ConsolidateReminders { get; set; }
    public string UserTimeZone { get; set; } = string.Empty;
    public bool RespectQuietHours { get; set; }
    public TimeOnly QuietHoursStart { get; set; }
    public TimeOnly QuietHoursEnd { get; set; }
}

public class UpdateReminderSettingsDto
{
    public int? DefaultReminderMinutesBefore { get; set; }
    public int? SecondReminderMinutesBefore { get; set; }
    public int? FollowUpHoursAfter { get; set; }
    public bool? EnableSessionReminders { get; set; }
    public bool? EnableSessionConfirmations { get; set; }
    public bool? EnableFollowUpReminders { get; set; }
    public bool? EnableFeedbackRequests { get; set; }
    public bool? EmailNotificationsEnabled { get; set; }
    public bool? SmsNotificationsEnabled { get; set; }
    public bool? PushNotificationsEnabled { get; set; }
    public int? MaxRemindersPerSession { get; set; }
    public bool? ConsolidateReminders { get; set; }
    public string? UserTimeZone { get; set; }
    public bool? RespectQuietHours { get; set; }
    public TimeOnly? QuietHoursStart { get; set; }
    public TimeOnly? QuietHoursEnd { get; set; }
}

public class BulkScheduleRemindersDto
{
    [Required]
    public List<int> SessionIds { get; set; } = new();

    [Required]
    public ReminderType ReminderType { get; set; }

    [Required]
    public DateTime ScheduleAt { get; set; }

    [StringLength(500)]
    public string? CustomSubject { get; set; }

    [StringLength(2000)]
    public string? CustomMessage { get; set; }
}

public class ReminderStatsDto
{
    public int TotalReminders { get; set; }
    public int ScheduledReminders { get; set; }
    public int SentReminders { get; set; }
    public int FailedReminders { get; set; }
    public int CancelledReminders { get; set; }
    public int PendingDelivery { get; set; }
    public double SuccessRate { get; set; }
    public Dictionary<ReminderType, int> RemindersByType { get; set; } = new();
    public List<MonthlyReminderStatsDto> MonthlyStats { get; set; } = new();
}

public class MonthlyReminderStatsDto
{
    public string Month { get; set; } = string.Empty; // Format: "2024-01"
    public int TotalSent { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
}
