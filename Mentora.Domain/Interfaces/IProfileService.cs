using Mentora.Domain.Models;

namespace Mentora.Domain.Interfaces;

public interface IProfileService
{
    Task<UserProfileDto> GetUserProfileAsync(string userId);
    Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateProfileRequest request);
    Task<bool> ValidateProfileImageAsync(Microsoft.AspNetCore.Http.IFormFile imageFile);
    Task<string> UploadProfileImageAsync(Microsoft.AspNetCore.Http.IFormFile imageFile, string userId);
}