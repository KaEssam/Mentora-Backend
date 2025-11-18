using Mentora.Core.Data;
using Mentora.Domain.Models;

namespace Mentora.Domain.Interfaces;

public interface IFileService
{
    Task<FileUploadResult> UploadFileAsync(FileUploadRequest request, string userId);
    Task<FileResponse?> GetFileByIdAsync(string id);
    Task<IEnumerable<FileResponse>> GetUserFilesAsync(string userId);
    Task<bool> DeleteFileAsync(string id, string userId);
    Task<FileResponse?> UpdateFileAsync(string id, FileUpdateRequest request, string userId);
    Task<bool> FileExistsAsync(string id);
    Task<IEnumerable<string>> GetAllowedFileTypesAsync();
    Task<long> GetMaxFileSizeAsync();
    Task<bool> ValidateFileAsync(Stream fileContent, string contentType, long fileSize);
}
