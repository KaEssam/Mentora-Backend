using Microsoft.EntityFrameworkCore;
using Mentora.Domain.Interfaces;
using Mentora.Core.Data;
using FileEntity = Mentora.Core.Data.File;

namespace Mentora.Infra.Data;

public class FileRepository : IFileRepository
{
    private readonly ApplicationDbContext _context;

    public FileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FileEntity?> GetByIdAsync(string id)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
    }

    public async Task<FileEntity> CreateAsync(FileEntity file)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<FileEntity> UpdateAsync(FileEntity file)
    {
        _context.Files.Update(file);
        await _context.SaveChangesAsync();
        return file;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var file = await _context.Files.FindAsync(id);
        if (file == null) return false;

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Files
            .AnyAsync(f => f.Id == id && f.IsActive);
    }

    public async Task<IEnumerable<FileEntity>> GetUserFilesAsync(string userId)
    {
        return await _context.Files
            .Where(f => f.UploadedById == userId && f.IsActive)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<FileEntity>> GetAllAsync()
    {
        return await _context.Files
            .Where(f => f.IsActive)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }
}