using Mentora.Core.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Mentora.Domain.DTOs
{
    public class CreateSessionDto
    {
        [Required]
        public DateTime StartAt { get; set; }

        public SessionType? Type { get; set; }

        [Required]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

        // Recurrence properties
        public bool IsRecurring { get; set; } = false;
        public RecurrenceDetails? Recurrence { get; set; }
    }

    public class ResponseSessionDto
    {
        public int Id { get; set; }
        public string MentorId { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public SessionStatus Status { get; set; }
        public SessionType Type { get; set; }
        public decimal Price { get; set; }
        public string? Notes { get; set; }

        // Recurrence properties
        public bool IsRecurring { get; set; }
        public RecurrenceDetails? Recurrence { get; set; }
        public int? ParentSessionId { get; set; }
    }

    public class CreateRecurringSessionDto
    {
        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan Duration { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public RecurrenceDetails Recurrence { get; set; }

        public SessionType? Type { get; set; }
        [StringLength(1000)]
        public string? Notes { get; set; }
    }

}
