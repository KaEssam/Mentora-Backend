using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Mentora.Domain.Interfaces;
using Mentora.Core.Data;
using Mentora.APIs.DTOs;
using Mentora.Infra.Data;

namespace Mentora.Infra.Services;

public class RoleService : IRoleService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleService> _logger;

    private static readonly Dictionary<UserRole, string> RoleMapping = new()
    {
        { UserRole.Mentee, "Mentee" },
        { UserRole.Mentor, "Mentor" },
        { UserRole.Admin, "Admin" }
    };

    public RoleService(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RoleService> logger)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    public async Task<bool> AssignRoleAsync(string userId, UserRole role, string assignedBy)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role assignment", userId);
                return false;
            }

            var roleName = RoleMapping[role];

            // Remove user from all existing roles first (to ensure single role per user)
            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
            {
                _logger.LogError("Failed to remove existing roles from user {UserId}: {Errors}",
                    userId, string.Join(", ", removeResult.Errors.Select(e => e.Description)));
                return false;
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, roleName);

            if (addResult.Succeeded)
            {
                _logger.LogInformation("User {UserId} assigned to role {RoleName} by {AssignedBy}",
                    userId, roleName, assignedBy);
                return true;
            }
            else
            {
                _logger.LogError("Failed to assign role {RoleName} to user {UserId}: {Errors}",
                    roleName, userId, string.Join(", ", addResult.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user {UserId}", role, userId);
            return false;
        }
    }

    public async Task<bool> RemoveRoleAsync(string userId, UserRole role, string removedBy)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role removal", userId);
                return false;
            }

            var roleName = RoleMapping[role];
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} removed from role {RoleName} by {RemovedBy}",
                    userId, roleName, removedBy);

                // Assign default Mentee role if user has no roles left
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Any())
                {
                    await _userManager.AddToRoleAsync(user, "Mentee");
                    _logger.LogInformation("User {UserId} assigned default Mentee role after role removal", userId);
                }

                return true;
            }
            else
            {
                _logger.LogError("Failed to remove role {RoleName} from user {UserId}: {Errors}",
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role {Role} from user {UserId}", role, userId);
            return false;
        }
    }

    public async Task<bool> ChangeUserRoleAsync(string userId, UserRole newRole, string changedBy, string? reason = null)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("User {UserId} not found for role change", userId);
                return false;
            }

            var roleName = RoleMapping[newRole];

            // Remove all existing roles
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            // Add new role
            var result = await _userManager.AddToRoleAsync(user, roleName);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {UserId} role changed to {RoleName} by {ChangedBy}. Reason: {Reason}",
                    userId, roleName, changedBy, reason ?? "Not specified");
                return true;
            }
            else
            {
                _logger.LogError("Failed to change role to {RoleName} for user {UserId}: {Errors}",
                    roleName, userId, string.Join(", ", result.Errors.Select(e => e.Description)));
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing role to {Role} for user {UserId}", newRole, userId);
            return false;
        }
    }

    public async Task<UserRole?> GetUserRoleAsync(string userId)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return null;

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault();

            return role switch
            {
                "Admin" => UserRole.Admin,
                "Mentor" => UserRole.Mentor,
                "Mentee" => UserRole.Mentee,
                _ => UserRole.Mentee // Default role
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting role for user {UserId}", userId);
            return null;
        }
    }



    public async Task<bool> UserHasRoleAsync(string userId, UserRole role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return false;

            var roleName = RoleMapping[role];
            return await _userManager.IsInRoleAsync(user, roleName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if user {UserId} has role {Role}", userId, role);
            return false;
        }
    }

    public async Task<IEnumerable<string>> GetUsersInRoleAsync(UserRole role)
    {
        try
        {
            var roleName = RoleMapping[role];
            var users = await _userManager.GetUsersInRoleAsync(roleName);
            return users.Select(u => u.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users in role {Role}", role);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<bool> IsRoleAssignmentValid(string userId, UserRole role)
    {
        // Add business logic for role validation
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return false;

        // TODO: INTEGRATION - Role Validation - Add business rules for role assignments
        // Example: Only admins can assign admin roles
        // Example: Mentors need to have certain qualifications

        return true;
    }

    public async Task<bool> SeedDefaultRolesAsync()
    {
        try
        {
            var roles = new[] { "Admin", "Mentor", "Mentee" };

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);

                    if (!result.Succeeded)
                    {
                        _logger.LogError("Failed to create role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                        return false;
                    }

                    _logger.LogInformation("Created default role: {RoleName}", roleName);
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error seeding default roles");
            return false;
        }
    }
}
