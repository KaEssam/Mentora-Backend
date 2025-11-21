using Microsoft.AspNetCore.Http;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;
using Mentora.Core.Data;

namespace Mentora.Domain.Services;

public class ProfileService : IProfileService
{
    private readonly IUserService _userService;
    private readonly IFileService _fileService;
    private readonly List<string> _allowedImageTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml"
    };
    private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB for profile images

    public ProfileService(IUserService userService, IFileService fileService)
    {
        _userService = userService;
        _fileService = fileService;
    }

    public async Task<UserProfileDto> GetUserProfileAsync(string userId)
    {
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");

        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Bio = user.Bio,
            ProfileImageUrl = user.ProfileImageUrl,
            Title = user.Title,
            Company = user.Company,
            Location = user.Location,
            Skills = user.Skills,
            Languages = user.Languages,
            ExperienceYears = user.ExperienceYears,
            Education = user.Education,
            SocialMedia = user.SocialMedia,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request)
    {
        // Get existing user
        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");

        // Debug: Log incoming request values
        Console.WriteLine($"UpdateProfileAsync called for user: {userId}");
        Console.WriteLine($"Request values - FirstName: '{request.FirstName}', LastName: '{request.LastName}', Bio: '{request.Bio}'");
        Console.WriteLine($"Request values - Title: '{request.Title}', Company: '{request.Company}', Location: '{request.Location}'");
        Console.WriteLine($"Request values - Skills: '{request.Skills}', Languages: '{request.Languages}', Education: '{request.Education}'");
        Console.WriteLine($"Request values - ExperienceYears: {request.ExperienceYears}, SocialMedia: '{request.SocialMedia}'");
        Console.WriteLine($"Has ProfileImage: {request.ProfileImage != null}");

        // Handle profile image upload if provided
        if (request.ProfileImage != null)
        {
            if (!await ValidateProfileImageAsync(request.ProfileImage))
                throw new ArgumentException("Invalid profile image");

            var imageUrl = await UploadProfileImageAsync(request.ProfileImage, userId);
            Console.WriteLine($"Upload successful. Image URL: {imageUrl}");
            user.ProfileImageUrl = imageUrl;
        }

        // Create a new user instance with only the fields that should be updated
        // This avoids Entity Framework tracking conflicts and only updates provided fields
        // Update user properties directly on the existing user object
        user.FirstName = (!string.IsNullOrEmpty(request.FirstName) && request.FirstName != "string") ? request.FirstName : user.FirstName;
        user.LastName = (!string.IsNullOrEmpty(request.LastName) && request.LastName != "string") ? request.LastName : user.LastName;
        user.Bio = (!string.IsNullOrEmpty(request.Bio) && request.Bio != "string") ? request.Bio : user.Bio;
        // ProfileImageUrl is already set above if image was uploaded
        user.Title = (!string.IsNullOrEmpty(request.Title) && request.Title != "string") ? request.Title : user.Title;
        user.Company = (!string.IsNullOrEmpty(request.Company) && request.Company != "string") ? request.Company : user.Company;
        user.Location = (!string.IsNullOrEmpty(request.Location) && request.Location != "string") ? request.Location : user.Location;
        user.Skills = (!string.IsNullOrEmpty(request.Skills) && request.Skills != "string") ? request.Skills : user.Skills;
        user.Languages = (!string.IsNullOrEmpty(request.Languages) && request.Languages != "string") ? request.Languages : user.Languages;
        user.Education = (!string.IsNullOrEmpty(request.Education) && request.Education != "string") ? request.Education : user.Education;
        user.SocialMedia = (!string.IsNullOrEmpty(request.SocialMedia) && request.SocialMedia != "string") ? request.SocialMedia : user.SocialMedia;

        // Only update ExperienceYears if it's greater than 0 (since 0 might be default)
        user.ExperienceYears = (request.ExperienceYears.HasValue && request.ExperienceYears.Value > 0)
            ? request.ExperienceYears.Value
            : user.ExperienceYears;

        user.UpdatedAt = DateTime.UtcNow;

        // Debug: Check what we're about to save
        Console.WriteLine($"About to update user. ProfileImageUrl: {user.ProfileImageUrl}");

        // Save user changes
        var updatedUser = await _userService.UpdateUserAsync(user);

        // Debug: Check what was returned
        Console.WriteLine($"Update complete. Returned ProfileImageUrl: {updatedUser.ProfileImageUrl}");

        // Return updated profile
        return new UserProfileDto
        {
            Id = updatedUser.Id,
            Email = updatedUser.Email,
            FirstName = updatedUser.FirstName,
            LastName = updatedUser.LastName,
            Bio = updatedUser.Bio,
            ProfileImageUrl = updatedUser.ProfileImageUrl,
            Title = updatedUser.Title,
            Company = updatedUser.Company,
            Location = updatedUser.Location,
            Skills = updatedUser.Skills,
            Languages = updatedUser.Languages,
            ExperienceYears = updatedUser.ExperienceYears,
            Education = updatedUser.Education,
            SocialMedia = updatedUser.SocialMedia,
            CreatedAt = updatedUser.CreatedAt,
            UpdatedAt = updatedUser.UpdatedAt
        };
    }

    public async Task<bool> ValidateProfileImageAsync(IFormFile imageFile)
    {
        if (imageFile == null || imageFile.Length == 0)
            return false;

        // Check file size
        if (imageFile.Length > _maxFileSize)
            return false;

        // Check file type
        if (!_allowedImageTypes.Contains(imageFile.ContentType))
            return false;

        return true;
    }

    public async Task<string> UploadProfileImageAsync(IFormFile imageFile, string userId)
    {
        // User validation should be handled by the calling method
        // Skip user validation here to avoid Entity Framework tracking conflicts

        Console.WriteLine($"UploadProfileImageAsync called. File: {imageFile.FileName}, Size: {imageFile.Length}, Type: {imageFile.ContentType}");

        using var stream = imageFile.OpenReadStream();

        var fileRequest = new Models.FileUploadRequest
        {
            FileContent = stream,
            FileName = imageFile.FileName,
            ContentType = imageFile.ContentType,
            FileSize = imageFile.Length,
            Description = "Profile image",
            Tags = "profile"
        };

        try
        {
            var uploadResult = await _fileService.UploadFileAsync(fileRequest, userId);
            Console.WriteLine($"File service returned URL: {uploadResult.Url}");
            return uploadResult.Url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"File upload failed: {ex.Message}");
            throw;
        }
    }
}
