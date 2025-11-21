using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mentora.Core.Data;

public class Session
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string MentorId { get; set; } = string.Empty;

    [Required]
    public DateTime StartAt { get; set; }

    [Required]
    public DateTime EndAt { get; set; }

    [Required]
    public SessionStatus Status { get; set; }

    [Required]
    public SessionType Type { get; set; }

    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    // Navigation properties will be configured in Infrastructure layer
    // to avoid circular dependencies in the Core layer
    // TODO: INTEGRATION - Navigation Properties - Consider using specification pattern or queries to avoid navigation properties in Core entities
}
