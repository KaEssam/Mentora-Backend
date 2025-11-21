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

    public async Task<ApplicationUser?> GetByIdAsync(string id)
    {
        return await _userManager.FindByIdAsync(id);
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser> CreateAsync(ApplicationUser user)
    {
        var result = await _userManager.CreateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    public async Task<ApplicationUser> CreateWithPasswordAsync(ApplicationUser user, string password)
    {
        var result = await _userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    public async Task<ApplicationUser> UpdateAsync(ApplicationUser user)
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
        // TODO: INTEGRATION - Social Media Enhancement - Add social media verification and linking when social media integration is implemented
        existingApplicationUser.UpdatedAt = DateTime.UtcNow;

        // Update the tracked entity
        var result = await _userManager.UpdateAsync(existingApplicationUser);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Failed to update user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return existingApplicationUser;
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

    public async Task<IEnumerable<ApplicationUser>> GetAllAsync()
    {
        return await _userManager.Users.ToListAsync();
    }
}
