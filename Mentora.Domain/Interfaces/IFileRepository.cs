using Mentora.Core.Data;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Domain.Interfaces;

public interface IFileRepository
{
    Task<FileEntity?> GetByIdAsync(string id);
    Task<FileEntity> CreateAsync(FileEntity file);
    Task<FileEntity> UpdateAsync(FileEntity file);
    Task<bool> DeleteAsync(string id);
    Task<bool> ExistsAsync(string id);
    Task<IEnumerable<FileEntity>> GetUserFilesAsync(string userId);
    Task<IEnumerable<FileEntity>> GetAllAsync();
}