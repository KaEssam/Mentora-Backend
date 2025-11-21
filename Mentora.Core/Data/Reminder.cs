using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mentora.Core.Data;

public class Reminder
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public int SessionId { get; set; }

    [Required]
    public ReminderType Type { get; set; }

    [Required]
    public DateTime ScheduledAt { get; set; }

    public DateTime? SentAt { get; set; }

    [Required]
    public ReminderStatus Status { get; set; } = ReminderStatus.Scheduled;

    [StringLength(500)]
    public string? Subject { get; set; }

    [StringLength(2000)]
    public string? Message { get; set; }

    [StringLength(100)]
    public string? RecipientEmail { get; set; }

    // Delivery information
    public int DeliveryAttempts { get; set; } = 0;
    public DateTime? LastAttemptAt { get; set; }
    public DateTime? NextRetryAt { get; set; }
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Session Session { get; set; } = null!;

    // Helper methods
    public bool IsOverdue => DateTime.UtcNow > ScheduledAt.AddHours(1) && Status == ReminderStatus.Scheduled;

    public bool CanRetry => DeliveryAttempts < 3 && Status == ReminderStatus.Failed;

    public void MarkAsSent()
    {
        Status = ReminderStatus.Sent;
        SentAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = ReminderStatus.Failed;
        ErrorMessage = errorMessage;
        LastAttemptAt = DateTime.UtcNow;
        DeliveryAttempts++;

        // Schedule retry with exponential backoff
        if (CanRetry)
        {
            var retryDelay = TimeSpan.FromMinutes(Math.Pow(2, DeliveryAttempts));
            NextRetryAt = DateTime.UtcNow.Add(retryDelay);
        }

        UpdatedAt = DateTime.UtcNow;
    }
}

public enum ReminderType
{
    SessionReminder = 1,
    SessionConfirmation = 2,
    SessionCancellation = 3,
    SessionRescheduled = 4,
    FollowUp = 5,
    FeedbackRequest = 6
}

public enum ReminderStatus
{
    Scheduled = 1,
    Sent = 2,
    Failed = 3,
    Cancelled = 4
}
