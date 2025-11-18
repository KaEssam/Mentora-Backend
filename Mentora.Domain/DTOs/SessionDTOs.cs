using Mentora.Core.Data;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Mentora.APIs.DTOs
{
    public class CreateSessionDto
    {

        [Required]
        public DateTime StartAt { get; set; }

        //[Required]
        //public DateTime EndAt { get; set; }

        //[Required]
        public SessionType? Type { get; set; }

        [Required]
        public decimal Price { get; set; }

        [StringLength(1000)]
        public string? Notes { get; set; }

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
    }
    
}
