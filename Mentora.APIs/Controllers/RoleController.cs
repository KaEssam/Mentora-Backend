using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;
using AutoMapper;
using Mentora.Core.Data;
using Mentora.APIs.DTOs;
using Mentora.Infra.Data;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace Mentora.APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RoleController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<RoleController> _logger;
    private readonly IMapper _mapper;

    public RoleController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<RoleController> logger,
        IMapper mapper)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpPost("assign")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> AssignRole([FromBody] RoleAssignmentRequest request)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            // Check if user already has this role
            var currentRoles = await _userManager.GetRolesAsync(user);
            var roleName = request.Role.ToString();

            if (currentRoles.Contains(roleName))
                return BadRequest(new { message = $"User already has role: {request.Role}" });

            // Remove all existing roles (simplified approach)
            var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeResult.Succeeded)
            {
                return BadRequest(new {
                    message = "Failed to remove existing roles",
                    errors = removeResult.Errors.Select(e => e.Description)
                });
            }

            // Add new role
            var addResult = await _userManager.AddToRoleAsync(user, roleName);
            if (!addResult.Succeeded)
            {
                return BadRequest(new {
                    message = "Failed to assign role",
                    errors = addResult.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation($"Admin assigned role {request.Role} to user {user.Email}");

            return Ok(new {
                message = $"Role {request.Role} assigned successfully to user {user.Email}",
                userId = user.Id,
                role = request.Role,
                roleName = request.Role.ToDisplayName()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning role to user");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetUserRoles(string userId)
    {
        try
        {
            // Only allow users to view their own roles or admins to view any roles
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isAdmin = User.IsInRole("Admin");

            if (currentUserId != userId && !isAdmin)
                return Forbid();

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var roles = await _userManager.GetRolesAsync(user);
            var userRoles = roles.Select(r => Enum.Parse<UserRole>(r)).ToList();

            var response = new RoleResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = userRoles.FirstOrDefault(),
                RoleName = userRoles.FirstOrDefault()?.ToDisplayName() ?? "No Role",
                RoleDescription = userRoles.FirstOrDefault()?.ToDescription() ?? "No role assigned",
                AssignedAt = user.UpdatedAt
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user roles");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    [HttpGet("list")]
    public IActionResult GetAllRoles()
    {
        try
        {
            var roles = Enum.GetValues<UserRole>()
                .Select(role => new UserRoleDto
                {
                    Role = role,
                    Name = role.ToDisplayName(),
                    Description = role.ToDescription()
                })
                .ToList();

            return Ok(roles);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving role list");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    [HttpGet("users/by-role/{role}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> GetUsersByRole(UserRole role)
    {
        try
        {
            var roleName = role.ToString();
            var usersInRole = await _userManager.GetUsersInRoleAsync(roleName);

            var userResponses = usersInRole.Select(user => new RoleResponse
            {
                UserId = user.Id,
                Email = user.Email ?? string.Empty,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = role,
                RoleName = role.ToDisplayName(),
                RoleDescription = role.ToDescription(),
                AssignedAt = user.UpdatedAt
            }).ToList();

            return Ok(userResponses);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users by role");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    [HttpDelete("remove/{userId}/{role}")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> RemoveRole(string userId, UserRole role)
    {
        try
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { message = "User not found" });

            var roleName = role.ToString();
            var result = await _userManager.RemoveFromRoleAsync(user, roleName);

            if (!result.Succeeded)
            {
                return BadRequest(new {
                    message = "Failed to remove role",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

            _logger.LogInformation($"Admin removed role {role} from user {user.Email}");

            return Ok(new {
                message = $"Role {role} removed successfully from user {user.Email}",
                userId = user.Id,
                removedRole = role
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }

    [HttpPost("initialize")]
    [Authorize(Policy = "AdminPolicy")]
    public async Task<IActionResult> InitializeRoles()
    {
        try
        {
            var roles = new[] { "Mentee", "Mentor", "Admin" };
            var createdRoles = new List<string>();

            foreach (var roleName in roles)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    var role = new IdentityRole(roleName);
                    var result = await _roleManager.CreateAsync(role);

                    if (result.Succeeded)
                    {
                        createdRoles.Add(roleName);
                        _logger.LogInformation($"Role {roleName} created successfully");
                    }
                    else
                    {
                        _logger.LogWarning($"Failed to create role {roleName}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }

            return Ok(new {
                message = "Role initialization completed",
                createdRoles = createdRoles,
                totalRoles = roles.Length
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing roles");
            return StatusCode(500, new { message = "An unexpected error occurred" });
        }
    }
    // TODO: INTEGRATION - Advanced Role Management - Add hierarchical roles and permissions when role-based access control is enhanced
}
