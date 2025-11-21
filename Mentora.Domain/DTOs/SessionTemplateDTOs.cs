using System.ComponentModel.DataAnnotations;
using Mentora.Core.Data;

namespace Mentora.Domain.DTOs;

public class CreateSessionTemplateDto
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Description { get; set; }

    [Required]
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
    public int MaximumDurationMinutes { get; set; } = 240;

    public RecurrenceDetails? DefaultRecurrence { get; set; }
}

public class UpdateSessionTemplateDto
{
    [StringLength(200)]
    public string? Name { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    public decimal? BasePrice { get; set; }

    public SessionType? Type { get; set; }

    public TimeSpan? DefaultDuration { get; set; }

    [StringLength(2000)]
    public string? DefaultNotes { get; set; }

    public bool? AllowRecurring { get; set; }
    public bool? AllowCustomDuration { get; set; }
    public bool? AllowCustomPrice { get; set; }
    public int? MinimumDurationMinutes { get; set; }
    public int? MaximumDurationMinutes { get; set; }

    public RecurrenceDetails? DefaultRecurrence { get; set; }
}

public class ResponseSessionTemplateDto
{
    public int Id { get; set; }
    public string MentorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal BasePrice { get; set; }
    public SessionType Type { get; set; }
    public TimeSpan DefaultDuration { get; set; }
    public string? DefaultNotes { get; set; }

    // Template configuration options
    public bool AllowRecurring { get; set; }
    public bool AllowCustomDuration { get; set; }
    public bool AllowCustomPrice { get; set; }
    public int MinimumDurationMinutes { get; set; }
    public int MaximumDurationMinutes { get; set; }

    public RecurrenceDetails? DefaultRecurrence { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int UsageCount { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class CreateSessionFromTemplateDto
{
    [Required]
    public int TemplateId { get; set; }

    [Required]
    public DateTime StartAt { get; set; }

    public TimeSpan? Duration { get; set; }

    public decimal? Price { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Override template settings
    public bool? IsRecurring { get; set; }
    public RecurrenceDetails? Recurrence { get; set; }
}

public class TemplateUsageStatsDto
{
    public int TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public int TotalUsage { get; set; }
    public DateTime? LastUsedAt { get; set; }
    public List<MonthlyUsageDto> MonthlyUsage { get; set; } = new();
}

public class MonthlyUsageDto
{
    public string Month { get; set; } = string.Empty; // Format: "2024-01"
    public int Count { get; set; }
}
