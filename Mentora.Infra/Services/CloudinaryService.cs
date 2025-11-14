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
        IOptions<CloudinarySettings> cloudinarySettings)
    {
        _fileRepository = fileRepository;
        _cloudinarySettings = cloudinarySettings.Value;

        // Log configuration for debugging (remove in production)
        Console.WriteLine($"Cloudinary Configuration:");
        Console.WriteLine($"  CloudName: {_cloudinarySettings.CloudName}");
        Console.WriteLine($"  ApiKey: {_cloudinarySettings.ApiKey?.Substring(0, Math.Min(8, _cloudinarySettings.ApiKey?.Length ?? 0))}...");
        Console.WriteLine($"  ApiSecret: {(!string.IsNullOrEmpty(_cloudinarySettings.ApiSecret) ? "***" : "NULL")}");

        // Validate configuration
        if (string.IsNullOrEmpty(_cloudinarySettings.CloudName))
            throw new ArgumentException("Cloudinary CloudName is not configured");

        if (string.IsNullOrEmpty(_cloudinarySettings.ApiKey))
            throw new ArgumentException("Cloudinary ApiKey is not configured");

        if (string.IsNullOrEmpty(_cloudinarySettings.ApiSecret))
            throw new ArgumentException("Cloudinary ApiSecret is not configured");

        var account = new Account(
            _cloudinarySettings.CloudName,
            _cloudinarySettings.ApiKey,
            _cloudinarySettings.ApiSecret);

        _cloudinary = new Cloudinary(account);
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, string userId)
    {
        // Skip user validation - it should be handled at the service layer
        // to avoid Entity Framework tracking conflicts

        // Validate file
        if (!await ValidateFileAsync(request.FileContent, request.ContentType, request.FileSize))
            throw new ArgumentException("Invalid file");

        // Upload to Cloudinary
        var publicId = $"{userId}/{Guid.NewGuid()}";

        try
        {
            RawUploadResult uploadResult;

            if (_allowedImageTypes.Contains(request.ContentType))
            {
                var imageUploadParams = new ImageUploadParams
                {
                    File = new FileDescription(request.FileName, request.FileContent),
                    PublicId = publicId,
                    Folder = "mentora/uploads",
                    Overwrite = true
                };
                uploadResult = await _cloudinary.UploadAsync(imageUploadParams);
            }
            else
            {
                var rawUploadParams = new RawUploadParams
                {
                    File = new FileDescription(request.FileName, request.FileContent),
                    PublicId = publicId,
                    Folder = "mentora/uploads",
                    Overwrite = true
                };
                uploadResult = await _cloudinary.UploadAsync(rawUploadParams);
            }

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException($"Failed to upload file to Cloudinary: {uploadResult.Error.Message}. Cloud name: {_cloudinarySettings.CloudName}");
            }

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
        catch (Exception ex)
        {
            // Log the full exception for debugging
            Console.WriteLine($"Cloudinary upload error: {ex.Message}");
            Console.WriteLine($"Cloud name: {_cloudinarySettings.CloudName}");
            Console.WriteLine($"API Key: {_cloudinarySettings.ApiKey?.Substring(0, Math.Min(8, _cloudinarySettings.ApiKey?.Length ?? 0))}...");

            throw new InvalidOperationException($"Failed to upload file to Cloudinary: {ex.Message}. Please check your Cloudinary configuration.", ex);
        }
    }

    public async Task<FileResponse?> GetFileByIdAsync(string id)
    {
        var file = await _fileRepository.GetByIdAsync(id);
        if (file == null) return null;

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
            UploadedByName = "User", // Will be populated at a higher level if needed
            UploadedAt = file.UploadedAt,
            UpdatedAt = file.UpdatedAt,
            IsActive = file.IsActive
        };
    }

    public async Task<IEnumerable<FileResponse>> GetUserFilesAsync(string userId)
    {
        var files = await _fileRepository.GetUserFilesAsync(userId);
        return files.Select(file => new FileResponse
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
            UploadedByName = "User", // Will be populated at a higher level if needed
            UploadedAt = file.UploadedAt,
            UpdatedAt = file.UpdatedAt,
            IsActive = file.IsActive
        });
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
            UploadedByName = "User", // Will be populated at a higher level if needed
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