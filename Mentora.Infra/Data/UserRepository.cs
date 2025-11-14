using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mentora.Domain.Interfaces;
using Mentora.Core.Data;

namespace Mentora.Infra.Data;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _context;

    public UserRepository(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    private static User ConvertToUser(ApplicationUser applicationUser)
    {
        return new User
        {
            Id = applicationUser.Id,
            Email = applicationUser.Email,
            FirstName = applicationUser.FirstName,
            LastName = applicationUser.LastName,
            Bio = applicationUser.Bio,
            ProfileImageUrl = applicationUser.ProfileImageUrl,
            Title = applicationUser.Title,
            Company = applicationUser.Company,
            Location = applicationUser.Location,
            CreatedAt = applicationUser.CreatedAt,
            UpdatedAt = applicationUser.UpdatedAt,
            Skills = applicationUser.Skills,
            Languages = applicationUser.Languages,
            ExperienceYears = applicationUser.ExperienceYears,
            Education = applicationUser.Education,
            SocialMedia = applicationUser.SocialMedia
        };
    }

    private static ApplicationUser ConvertToApplicationUser(User user)
    {
        return new ApplicationUser
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            Title = user.Title,
            Company = user.Company,
            Location = user.Location,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Skills = user.Skills,
            Languages = user.Languages,
            ExperienceYears = user.ExperienceYears,
            Education = user.Education,
            SocialMedia = user.SocialMedia
        };
    }

    public async Task<User?> GetByIdAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        return user != null ? ConvertToUser(user) : null;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        var user = await _userManager.FindByEmailAsync(email);
        return user != null ? ConvertToUser(user) : null;
    }

    public async Task<User> CreateAsync(User user)
    {
        var applicationUser = ConvertToApplicationUser(user);
        var result = await _userManager.CreateAsync(applicationUser);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return ConvertToUser(applicationUser);
    }

    public async Task<User> CreateWithPasswordAsync(User user, string password)
    {
        var applicationUser = ConvertToApplicationUser(user);
        var result = await _userManager.CreateAsync(applicationUser, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return ConvertToUser(applicationUser);
    }

    public async Task<User> UpdateAsync(User user)
    {
        // Get the existing ApplicationUser to avoid tracking conflicts
        var existingApplicationUser = await _userManager.FindByIdAsync(user.Id);
        if (existingApplicationUser == null)
        {
            throw new ArgumentException("User not found");
        }

        // Update properties on the existing tracked entity
        existingApplicationUser.FirstName = user.FirstName;
        existingApplicationUser.LastName = user.LastName;
        existingApplicationUser.Bio = user.Bio;
        existingApplicationUser.ProfileImageUrl = user.ProfileImageUrl;
        existingApplicationUser.Title = user.Title;
        existingApplicationUser.Company = user.Company;
        existingApplicationUser.Location = user.Location;
        existingApplicationUser.Skills = user.Skills;
        existingApplicationUser.Languages = user.Languages;
        existingApplicationUser.ExperienceYears = user.ExperienceYears;
        existingApplicationUser.Education = user.Education;
        existingApplicationUser.SocialMedia = user.SocialMedia;
        existingApplicationUser.UpdatedAt = DateTime.UtcNow;

        // Update the tracked entity
        var result = await _userManager.UpdateAsync(existingApplicationUser);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return ConvertToUser(existingApplicationUser);
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null) return false;

        var result = await _userManager.DeleteAsync(user);
        return result.Succeeded;
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _userManager.Users.AnyAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        var users = await _userManager.Users.ToListAsync();
        return users.Select(ConvertToUser);
    }
}