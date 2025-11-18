using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using Mentora.Domain.Models;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Domain.Services;

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IUserRepository _userRepository;
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

    public FileService(IFileRepository fileRepository, IUserRepository userRepository)
    {
        _fileRepository = fileRepository;
        _userRepository = userRepository;
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

        // Create file entity
        var fileEntity = new FileEntity
        {
            Id = Guid.NewGuid().ToString(),
            FileName = $"{Guid.NewGuid()}{Path.GetExtension(request.FileName)}",
            OriginalFileName = request.FileName,
            ContentType = request.ContentType,
            FileSize = request.FileSize,
            Description = request.Description,
            Tags = request.Tags,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        // Save to database
        var createdFile = await _fileRepository.CreateAsync(fileEntity);

        // Return result - URL will be set by infrastructure layer after cloud upload
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