using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.APIs.DTOs;
using Mentora.Domain.Models;

namespace Mentora.APIs.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IFileService _fileService;

    public UserController(IUserService userService, IFileService fileService)
    {
        _userService = userService;
        _fileService = fileService;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<User>> GetUser(string id)
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
    public async Task<ActionResult<User>> GetCurrentUser()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var user = await _userService.GetUserByIdAsync(userId);
            if (user == null)
                return NotFound(new { error = "User not found" });

            return Ok(user);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpPut("me")]
    public async Task<ActionResult<User>> UpdateCurrentUser([FromBody] UserUpdateRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var existingUser = await _userService.GetUserByIdAsync(userId);
            if (existingUser == null)
                return NotFound(new { error = "User not found" });

            // Update user properties
            existingUser.FirstName = request.FirstName ?? existingUser.FirstName;
            existingUser.LastName = request.LastName ?? existingUser.LastName;
            existingUser.Bio = request.Bio;
            existingUser.Title = request.Title;
            existingUser.Company = request.Company;
            existingUser.Location = request.Location;
            existingUser.Skills = request.Skills;
            existingUser.Languages = request.Languages;
            existingUser.ExperienceYears = request.ExperienceYears ?? existingUser.ExperienceYears;
            existingUser.Education = request.Education;
            existingUser.SocialMedia = request.SocialMedia;
            existingUser.UpdatedAt = DateTime.UtcNow;

            var updatedUser = await _userService.UpdateUserAsync(existingUser);
            return Ok(updatedUser);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpPost("upload-profile-image")]
    public async Task<ActionResult<FileDto.FileUploadResult>> UploadProfileImage([FromForm] FileDto.FileUploadRequest request)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            // Validate file
            if (!request.File.ContentType.StartsWith("image/"))
                return BadRequest(new { error = "Only image files are allowed for profile pictures" });

            // Convert API DTO to Domain model
            using var stream = request.File.OpenReadStream();
            var domainRequest = new FileUploadRequest
            {
                FileContent = stream,
                FileName = request.File.FileName,
                ContentType = request.File.ContentType,
                FileSize = request.File.Length,
                Description = request.Description,
                Tags = request.Tags
            };

            var result = await _fileService.UploadFileAsync(domainRequest, userId);

            // Update user's profile image URL
            var user = await _userService.GetUserByIdAsync(userId);
            if (user != null)
            {
                user.ProfileImageUrl = result.Url;
                await _userService.UpdateUserAsync(user);
            }

            // Convert Domain result to API DTO
            var apiResult = new FileDto.FileUploadResult
            {
                Id = result.Id,
                FileName = result.FileName,
                OriginalFileName = result.OriginalFileName,
                ContentType = result.ContentType,
                FileSize = result.FileSize,
                Url = result.Url,
                Description = result.Description,
                Tags = result.Tags,
                UploadedAt = result.UploadedAt
            };

            return Ok(apiResult);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred during file upload" });
        }
    }

    [HttpGet("files")]
    public async Task<ActionResult<IEnumerable<FileDto.FileResponse>>> GetUserFiles()
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var files = await _fileService.GetUserFilesAsync(userId);

            // Convert Domain models to API DTOs
            var apiFiles = files.Select(f => new FileDto.FileResponse
            {
                Id = f.Id,
                FileName = f.FileName,
                OriginalFileName = f.OriginalFileName,
                ContentType = f.ContentType,
                FileSize = f.FileSize,
                Url = f.Url,
                Description = f.Description,
                Tags = f.Tags,
                UploadedById = f.UploadedById,
                UploadedByName = f.UploadedByName,
                UploadedAt = f.UploadedAt,
                UpdatedAt = f.UpdatedAt,
                IsActive = f.IsActive
            });

            return Ok(apiFiles);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpGet("files/{id}")]
    public async Task<ActionResult<FileDto.FileResponse>> GetFile(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var file = await _fileService.GetFileByIdAsync(id);
            if (file == null)
                return NotFound(new { error = "File not found" });

            // Ensure user can only access their own files
            if (file.UploadedById != userId)
                return Forbid();

            // Convert Domain model to API DTO
            var apiFile = new FileDto.FileResponse
            {
                Id = file.Id,
                FileName = file.FileName,
                OriginalFileName = file.OriginalFileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                Url = file.Url,
                Description = file.Description,
                Tags = file.Tags,
                UploadedById = file.UploadedById,
                UploadedByName = file.UploadedByName,
                UploadedAt = file.UploadedAt,
                UpdatedAt = file.UpdatedAt,
                IsActive = file.IsActive
            };

            return Ok(apiFile);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpDelete("files/{id}")]
    public async Task<ActionResult> DeleteFile(string id)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { error = "User not authenticated" });

            var success = await _fileService.DeleteFileAsync(id, userId);
            if (!success)
                return NotFound(new { error = "File not found or access denied" });

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "An unexpected error occurred" });
        }
    }

    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<User>>> SearchUsers([FromQuery] string? query = null, [FromQuery] string? skills = null, [FromQuery] string? location = null)
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

public class UserUpdateRequest
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Bio { get; set; }
    public string? Title { get; set; }
    public string? Company { get; set; }
    public string? Location { get; set; }
    public string? Skills { get; set; }
    public string? Languages { get; set; }
    public int? ExperienceYears { get; set; }
    public string? Education { get; set; }
    public string? SocialMedia { get; set; }
}
