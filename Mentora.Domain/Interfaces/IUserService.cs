using Mentora.Domain.Models;

namespace Mentora.Domain.Interfaces;

public interface IUserService
{
    Task<IUser?> GetUserByIdAsync(string id);
    Task<IUser?> GetUserByEmailAsync(string email);
    Task<IUser> CreateUserAsync(IUser user, string password);
    Task<IUser> UpdateUserAsync(IUser user);
    Task<bool> DeleteUserAsync(string id);
    Task<bool> ValidateUserCredentials(string email, string password);
    Task<bool> UserExistsAsync(string email);
    Task<IEnumerable<IUser>> GetAllUsersAsync();
}
