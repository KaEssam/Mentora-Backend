using Mentora.Domain.Interfaces;
using Mentora.Core.Data;

namespace Mentora.Domain.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;

    public UserService(IUserRepository userRepository, IJwtService jwtService)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
    }

    public async Task<ApplicationUser?> GetUserByIdAsync(string id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<ApplicationUser?> GetUserByEmailAsync(string email)
    {
        return await _userRepository.GetByEmailAsync(email);
    }

    public async Task<ApplicationUser> CreateUserAsync(ApplicationUser user, string password)
    {
        // Business validation
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long.");
        }

        if (await UserExistsAsync(user.Email!))
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        user.CreatedAt = DateTime.UtcNow;
        user.UpdatedAt = DateTime.UtcNow;

        return await _userRepository.CreateWithPasswordAsync(user, password);
    }

    public async Task<ApplicationUser> UpdateUserAsync(ApplicationUser user)
    {
        var existingUser = await _userRepository.GetByIdAsync(user.Id);
        if (existingUser == null)
        {
            throw new ArgumentException("User not found.");
        }

        // Business validation for email updates
        if (user.Email != existingUser.Email && await UserExistsAsync(user.Email!))
        {
            throw new InvalidOperationException("Email is already in use.");
        }

        user.UpdatedAt = DateTime.UtcNow;
        return await _userRepository.UpdateAsync(user);
    }

    public async Task<bool> DeleteUserAsync(string id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null)
        {
            return false;
        }

        return await _userRepository.DeleteAsync(id);
    }

    public async Task<bool> ValidateUserCredentials(string email, string password)
    {
        var user = await _userRepository.GetByEmailAsync(email);
        if (user == null)
        {
            return false;
        }

        // In a real implementation, you would hash the password and compare
        // For now, this is a placeholder that would be handled by the infrastructure layer
        return true;
    }

    public async Task<bool> UserExistsAsync(string email)
    {
        return await _userRepository.ExistsByEmailAsync(email);
    }

    public async Task<IEnumerable<ApplicationUser>> GetAllUsersAsync()
    {
        return await _userRepository.GetAllAsync();
    }
}
