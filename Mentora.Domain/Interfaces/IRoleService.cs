using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface IRoleService
{
    Task<bool> AssignRoleAsync(string userId, UserRole role, string assignedBy);
    Task<bool> RemoveRoleAsync(string userId, UserRole role, string removedBy);
    Task<bool> ChangeUserRoleAsync(string userId, UserRole newRole, string changedBy, string? reason = null);
    Task<UserRole?> GetUserRoleAsync(string userId);
    Task<bool> UserHasRoleAsync(string userId, UserRole role);
    Task<bool> IsRoleAssignmentValid(string userId, UserRole role);
    Task<bool> SeedDefaultRolesAsync();
}
