using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Infra.Services;

public class LocalFileService : IFileService
{
    private readonly string _uploadsBasePath;
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

    public LocalFileService()
    {
        // Use a relative path for uploads - this will be relative to the API project
        _uploadsBasePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

        // Ensure uploads directory exists
        if (!Directory.Exists(_uploadsBasePath))
        {
            Directory.CreateDirectory(_uploadsBasePath);
        }
    }

    public async Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, string userId)
    {
        // Skip user validation - it should be handled at the service layer
        // to avoid Entity Framework tracking conflicts

        // Validate file
        if (!await ValidateFileAsync(request.FileContent, request.ContentType, request.FileSize))
            throw new ArgumentException("Invalid file");

        // Create unique filename
        var fileExtension = Path.GetExtension(request.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var userDirectory = Path.Combine(_uploadsBasePath, userId);
        var fullPath = Path.Combine(userDirectory, uniqueFileName);

        // Ensure user directory exists
        if (!Directory.Exists(userDirectory))
        {
            Directory.CreateDirectory(userDirectory);
        }

        // Save file to local storage
        using (var fileStream = new FileStream(fullPath, FileMode.Create))
        {
            await request.FileContent.CopyToAsync(fileStream);
        }

        // Create file URL (relative path for web access)
        var relativePath = Path.Combine("uploads", userId, uniqueFileName).Replace("\\", "/");
        var fileUrl = $"/{relativePath}";

        // Create a fake database ID for now (using GUID)
        var fileId = Guid.NewGuid().ToString();

        Console.WriteLine($"File saved locally: {fullPath}");
        Console.WriteLine($"File URL: {fileUrl}");

        return new FileUploadResult
        {
            Id = fileId,
            FileName = uniqueFileName,
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            Url = fileUrl,
            Description = request.Description,
            Tags = request.Tags,
            UploadedAt = DateTime.UtcNow
        };
    }

    public async Task<FileResponse?> GetFileByIdAsync(string id)
    {
        // For now, return null since we're not storing in database
        // This can be implemented later when database table is ready
        return null;
    }

    public async Task<IEnumerable<FileResponse>> GetUserFilesAsync(string userId)
    {
        // For now, return empty list since we're not storing in database
        // This can be implemented later when database table is ready
        return new List<FileResponse>();
    }

    public async Task<bool> DeleteFileAsync(string id, string userId)
    {
        // For now, return false since we're not tracking files in database
        // This can be implemented later when database table is ready
        return false;
    }

    public async Task<FileResponse?> UpdateFileAsync(string id, FileUpdateRequest request, string userId)
    {
        // For now, return null since we're not storing in database
        // This can be implemented later when database table is ready
        return null;
    }

    public async Task<bool> FileExistsAsync(string id)
    {
        // For now, return false since we're not tracking files in database
        // This can be implemented later when database table is ready
        return false;
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
        // Check file size
        if (fileSize > _maxFileSize)
            return false;

        // Check file type
        var allAllowedTypes = _allowedImageTypes.Concat(_allowedDocumentTypes);
        if (!allAllowedTypes.Contains(contentType))
            return false;

        return true;
    }
}