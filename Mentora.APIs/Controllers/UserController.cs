using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;
using Mentora.Core.Data;

namespace Mentora.APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IProfileService _profileService;

    public UserController(IUserService userService, IProfileService profileService)
    {
        _userService = userService;
        _profileService = profileService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApplicationUser>> GetUser(string id)
    {
        try
        {
            var user = await _userService.GetUserByIdAsync(id);
            if (user == null)
                return NotFound(new { error = "User not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var profile = await _profileService.GetUserProfileAsync(userId);
            return Ok(profile);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpPut("profile")]
    public async Task<ActionResult<UserProfileDto>> UpdateProfile([FromForm] UpdateProfileRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var updatedProfile = await _profileService.UpdateProfileAsync(userId, request);
            return Ok(updatedProfile);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            // Log the full exception for debugging
            Console.WriteLine($"Profile update error: {ex}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");

            return StatusCode(500, new
            {
                error = "An unexpected error occurred",
                details = ex.Message
            });
        }
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<ApplicationUser>>> SearchUsers([FromQuery] string? query = null, [FromQuery] string? skills = null, [FromQuery] string? location = null)
    // TODO: INTEGRATION - Advanced Search - Add Elasticsearch integration for improved search performance and relevance
    {
        try
        {
            var users = await _userService.GetAllUsersAsync();

            // Apply filters
            if (!string.IsNullOrEmpty(query))
            {
                var lowerQuery = query.ToLower();
                users = users.Where(u =>
                    (u.FirstName?.ToLower().Contains(lowerQuery) ?? false) ||
                    (u.LastName?.ToLower().Contains(lowerQuery) ?? false) ||
                    (u.Bio?.ToLower().Contains(lowerQuery) ?? false) ||
                    (u.Title?.ToLower().Contains(lowerQuery) ?? false));
            }

            if (!string.IsNullOrEmpty(skills))
            {
                var lowerSkills = skills.ToLower();
                users = users.Where(u => u.Skills?.ToLower().Contains(lowerSkills) ?? false);
            }

            if (!string.IsNullOrEmpty(location))
            {
                var lowerLocation = location.ToLower();
                users = users.Where(u => u.Location?.ToLower().Contains(lowerLocation) ?? false);
            }

            return Ok(users.ToList());
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }
}
