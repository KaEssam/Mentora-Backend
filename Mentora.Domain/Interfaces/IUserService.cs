using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IUserService
{
    Task<User?> GetUserByIdAsync(string id);
    Task<User?> GetUserByEmailAsync(string email);
    Task<User> CreateUserAsync(User user, string password);
    Task<User> UpdateUserAsync(User user);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> ValidateUserCredentials(string email, string password);
    Task<bool> UserExistsAsync(string email);
    Task<IEnumerable<User>> GetAllUsersAsync();
}