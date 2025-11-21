using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mentora.Core.Data;

public class Booking
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public int SessionId { get; set; }

    [Required]
    [StringLength(450)]
    public string MentorId { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string MenteeId { get; set; } = string.Empty;

    [Required]
    public SessionStatus Status { get; set; }

    [Required]
    public SessionType Type { get; set; }

    [StringLength(500)]
    public string? MeetingUrl { get; set; }

    // Navigation properties will be configured in Infrastructure layer
    // to avoid circular references with Core layer
    // TODO: INTEGRATION - Navigation Properties - Consider using specification pattern or queries to avoid navigation properties in Core entities
}
