using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Infra.Services;

public class CloudinaryService : IFileService
{
    private readonly Cloudinary _cloudinary;
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
    private readonly CloudinarySettings _cloudinarySettings;
    private readonly List<string> _allowedImageTypes = new()
    {
        "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp", "image/svg+xml"
    };
    private readonly List<string> _allowedDocumentTypes = new()
    {
        "application/pdf", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain", "text/csv"
    };
    private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

    public CloudinaryService(
        IFileRepository fileRepository,
        IUserRepository userRepository,
        IOptions<CloudinarySettings> cloudinarySettings)
    {
        _fileRepository = fileRepository;
        _userRepository = userRepository;
        _cloudinarySettings = cloudinarySettings.Value;

        var account = new Account(
            _cloudinarySettings.CloudName,
            _cloudinarySettings.ApiKey,
            _cloudinarySettings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, string userId)
    {
        // Validate user exists
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
            throw new ArgumentException("User not found");

        // Validate file
        if (!await ValidateFileAsync(request.FileContent, request.ContentType, request.FileSize))
            throw new ArgumentException("Invalid file");

        // Upload to Cloudinary
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(request.FileName, request.FileContent),
            PublicId = $"{userId}/{Guid.NewGuid()}",
            Folder = "mentora/uploads",
            Overwrite = true
        };

        // Use ImageUpload for image files, RawUpload for other files
        var uploadResult = _allowedImageTypes.Contains(request.ContentType)
            ? await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(request.FileName, request.FileContent),
                PublicId = $"{userId}/{Guid.NewGuid()}",
                Folder = "mentora/uploads",
                Overwrite = true
            })
            : await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
            throw new InvalidOperationException($"Failed to upload file to Cloudinary: {uploadResult.Error.Message}");

        // Create file entity
        var fileEntity = new FileEntity
        {
            Id = Guid.NewGuid().ToString(),
            FileName = uploadResult.PublicId,
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            FilePath = uploadResult.SecureUrl?.ToString(),
            PublicUrl = uploadResult.SecureUrl?.ToString(),
            CloudinaryPublicId = uploadResult.PublicId,
            Description = request.Description,
            Tags = request.Tags,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Save to database
        var createdFile = await _fileRepository.CreateAsync(fileEntity);

        return new FileUploadResult
        {
            Id = createdFile.Id,
            FileName = createdFile.FileName,
            OriginalFileName = createdFile.OriginalFileName,
            ContentType = createdFile.ContentType,
            FileSize = createdFile.FileSize,
            Url = createdFile.PublicUrl ?? string.Empty,
            Description = createdFile.Description,
            Tags = createdFile.Tags,
            UploadedAt = createdFile.UploadedAt
        };
    }

    public async Task<FileResponse?> GetFileByIdAsync(string id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null) return null;

        var user = await _userRepository.GetByIdAsync(file.UploadedById ?? string.Empty);

        return new FileResponse
        {
            Id = file.Id,
            FileName = file.FileName,
            OriginalFileName = file.OriginalFileName,
            ContentType = file.ContentType,
            FileSize = file.FileSize,
            Url = file.PublicUrl ?? string.Empty,
            Description = file.Description,
            Tags = file.Tags,
            UploadedById = file.UploadedById,
            UploadedByName = $"{user?.FirstName} {user?.LastName}",
            UploadedAt = file.UploadedAt,
            UpdatedAt = file.UpdatedAt,
            IsActive = file.IsActive
        };
    }

    public async Task<IEnumerable<FileResponse>> GetUserFilesAsync(string userId)
    {
        var files = await _fileRepository.GetUserFilesAsync(userId);
        var fileResponses = new List<FileResponse>();

        foreach (var file in files)
        {
            var user = await _userRepository.GetByIdAsync(file.UploadedById ?? string.Empty);
            fileResponses.Add(new FileResponse
            {
                Id = file.Id,
                FileName = file.FileName,
                OriginalFileName = file.OriginalFileName,
                ContentType = file.ContentType,
                FileSize = file.FileSize,
                Url = file.PublicUrl ?? string.Empty,
                Description = file.Description,
                Tags = file.Tags,
                UploadedById = file.UploadedById,
                UploadedByName = $"{user?.FirstName} {user?.LastName}",
                UploadedAt = file.UploadedAt,
                UpdatedAt = file.UpdatedAt,
                IsActive = file.IsActive
            });
        }

        return fileResponses;
    }

    public async Task<bool> DeleteFileAsync(string id, string userId)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null || file.UploadedById != userId)
            return false;

        // Delete from Cloudinary if we have a public ID
        if (!string.IsNullOrEmpty(file.CloudinaryPublicId))
        {
            var deletionParams = new DeletionParams(file.CloudinaryPublicId);
            var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

            if (deletionResult.Error != null && deletionResult.Error.Message != "not found")
            {
                // Log error but don't fail the deletion if Cloudinary deletion fails
                // In production, you might want to handle this differently
                Console.WriteLine($"Failed to delete from Cloudinary: {deletionResult.Error.Message}");
            }
        }

        // Delete from database
        return await _fileRepository.DeleteAsync(id);
    }

    public async Task<FileResponse?> UpdateFileAsync(string id, FileUpdateRequest request, string userId)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null || file.UploadedById != userId)
            return null;

        file.Description = request.Description;
        file.Tags = request.Tags;
        file.IsActive = request.IsActive;
        file.UpdatedAt = DateTime.UtcNow;

        var updatedFile = await _fileRepository.UpdateAsync(file);
        var user = await _userRepository.GetByIdAsync(updatedFile.UploadedById ?? string.Empty);

        return new FileResponse
        {
            Id = updatedFile.Id,
            FileName = updatedFile.FileName,
            OriginalFileName = updatedFile.OriginalFileName,
            ContentType = updatedFile.ContentType,
            FileSize = updatedFile.FileSize,
            Url = updatedFile.PublicUrl ?? string.Empty,
            Description = updatedFile.Description,
            Tags = updatedFile.Tags,
            UploadedById = updatedFile.UploadedById,
            UploadedByName = $"{user?.FirstName} {user?.LastName}",
            UploadedAt = updatedFile.UploadedAt,
            UpdatedAt = updatedFile.UpdatedAt,
            IsActive = updatedFile.IsActive
        };
    }

    public async Task<bool> FileExistsAsync(string id)
    {
        return await _fileRepository.ExistsAsync(id);
    }

    public async Task<IEnumerable<string>> GetAllowedFileTypesAsync()
    {
        return _allowedImageTypes.Concat(_allowedDocumentTypes);
    }

    public async Task<long> GetMaxFileSizeAsync()
    {
        return _maxFileSize;
    }

    public async Task<bool> ValidateFileAsync(Stream fileContent, string contentType, long fileSize)
    {
        if (fileContent == null || fileSize == 0)
            return false;

        if (fileSize > _maxFileSize)
            return false;

        var allowedTypes = await GetAllowedFileTypesAsync();
        return allowedTypes.Contains(contentType);
    }
}