namespace Mentora.Core.Data;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? Skills { get; set; }
    public string? Languages { get; set; }
    public int ExperienceYears { get; set; }
    public string? Education { get; set; }
    public string? SocialMedia { get; set; }

    // Navigation properties
    public virtual ICollection<Session> MentorSessions { get; set; } = new List<Session>();
    public virtual ICollection<Booking> MentorBookings { get; set; } = new List<Booking>();
    public virtual ICollection<Booking> MenteeBookings { get; set; } = new List<Booking>();
}