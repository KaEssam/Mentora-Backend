namespace Mentora.Domain.Models;

public interface IUser
{
    string Id { get; }
    string? Email { get; }
    string FirstName { get; }
    string LastName { get; }
    string? Bio { get; }
    string? ProfileImageUrl { get; }
    string? Title { get; }
    string? Company { get; }
    string? Location { get; }
    DateTime CreatedAt { get; }
    DateTime UpdatedAt { get; }
    string? Skills { get; }
    string? Languages { get; }
    int ExperienceYears { get; }
    string? Education { get; }
    string? SocialMedia { get; }
}
