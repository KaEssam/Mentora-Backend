namespace Mentora.Domain.Models;

public interface IUser
{
    string Id { get; }
    string? Email { get; set; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string? Bio { get; set; }
    string? ProfileImageUrl { get; set; }
    string? Title { get; set; }
    string? Company { get; set; }
    string? Location { get; set; }
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
    string? Skills { get; set; }
    string? Languages { get; set; }
    int ExperienceYears { get; set; }
    string? Education { get; set; }
    string? SocialMedia { get; set; }
}
