using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace Mentora.Core.Data;

public class SessionTemplate
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string MentorId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Required]
    public SessionType Type { get; set; }

    public TimeSpan DefaultDuration { get; set; } = TimeSpan.FromMinutes(60);

    [StringLength(2000)]
    public string? DefaultNotes { get; set; }

    // Template configuration options
    public bool AllowRecurring { get; set; } = false;
    public bool AllowCustomDuration { get; set; } = true;
    public bool AllowCustomPrice { get; set; } = true;
    public int MinimumDurationMinutes { get; set; } = 30;
    public int MaximumDurationMinutes { get; set; } = 240; // 4 hours

    // Serialized default recurrence if applicable
    public string? DefaultRecurrenceJson { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Template usage statistics
    public int UsageCount { get; set; } = 0;
    public DateTime? LastUsedAt { get; set; }

    // Note: Navigation properties are configured in Infrastructure layer
    // to maintain Clean Architecture principles

    // Method to get recurrence details
    public RecurrenceDetails? GetDefaultRecurrence()
    {
        if (string.IsNullOrEmpty(DefaultRecurrenceJson))
            return null;

        return JsonSerializer.Deserialize<RecurrenceDetails>(DefaultRecurrenceJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
    }

    // Method to set recurrence details
    public void SetDefaultRecurrence(RecurrenceDetails? recurrence)
    {
        if (recurrence == null)
        {
            DefaultRecurrenceJson = null;
        }
        else
        {
            DefaultRecurrenceJson = JsonSerializer.Serialize(recurrence, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
