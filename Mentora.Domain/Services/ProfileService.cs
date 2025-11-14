using Microsoft.AspNetCore.Http;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;

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

        // Handle profile image upload if provided
        if (request.ProfileImage != null)
        {
            if (!await ValidateProfileImageAsync(request.ProfileImage))
                throw new ArgumentException("Invalid profile image");

            var imageUrl = await UploadProfileImageAsync(request.ProfileImage, userId);
            user.ProfileImageUrl = imageUrl;
        }

        // Create a new user instance with only the fields we want to update
        // This avoids Entity Framework tracking conflicts
        var userToUpdate = new User
        {
            Id = user.Id,
            Email = user.Email, // Email shouldn't change in profile update
            FirstName = request.FirstName ?? user.FirstName,
            LastName = request.LastName ?? user.LastName,
            Bio = request.Bio ?? user.Bio,
            ProfileImageUrl = user.ProfileImageUrl, // Already set above if image was uploaded
            Title = request.Title ?? user.Title,
            Company = request.Company ?? user.Company,
            Location = request.Location ?? user.Location,
            Skills = request.Skills ?? user.Skills,
            Languages = request.Languages ?? user.Languages,
            ExperienceYears = request.ExperienceYears ?? user.ExperienceYears,
            Education = request.Education ?? user.Education,
            SocialMedia = request.SocialMedia ?? user.SocialMedia,
            CreatedAt = user.CreatedAt,
            UpdatedAt = DateTime.UtcNow
        };

        // Save user changes
        var updatedUser = await _userService.UpdateUserAsync(userToUpdate);

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

        var uploadResult = await _fileService.UploadFileAsync(fileRequest, userId);
        return uploadResult.Url;
    }
}