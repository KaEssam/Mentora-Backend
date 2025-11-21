using Mentora.Domain.Models;

namespace Mentora.Domain.Interfaces;

public interface IUserRepository
{
    Task<IUser?> GetByIdAsync(string id);
    Task<IUser?> GetByEmailAsync(string email);
    Task<IUser> CreateAsync(IUser user, string password);
    Task<IUser> UpdateAsync(IUser user);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsByEmailAsync(string email);
    Task<IEnumerable<IUser>> GetAllAsync();
}
