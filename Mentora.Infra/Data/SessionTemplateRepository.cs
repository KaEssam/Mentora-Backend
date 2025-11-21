using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Data;

public class SessionTemplateRepository : ISessionTemplateRepository
{
    private readonly ApplicationDbContext _context;

    public SessionTemplateRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SessionTemplate?> GetByIdAsync(int id)
    {
        return await _context.SessionTemplates
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<SessionTemplate>> GetByMentorIdAsync(string mentorId)
    {
        return await _context.SessionTemplates
            .Where(t => t.MentorId == mentorId)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionTemplate>> GetActiveTemplatesByMentorIdAsync(string mentorId)
    {
        return await _context.SessionTemplates
            .Where(t => t.MentorId == mentorId && t.IsActive)
            .OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionTemplate>> GetAllActiveTemplatesAsync()
    {
        return await _context.SessionTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.UsageCount)
            .ToListAsync();
    }

    public async Task<SessionTemplate> CreateAsync(SessionTemplate template)
    {
        _context.SessionTemplates.Add(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<SessionTemplate> UpdateAsync(SessionTemplate template)
    {
        _context.SessionTemplates.Update(template);
        await _context.SaveChangesAsync();
        return template;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var template = await _context.SessionTemplates.FindAsync(id);
        if (template == null) return false;

        _context.SessionTemplates.Remove(template);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.SessionTemplates.AnyAsync(t => t.Id == id);
    }

    public async Task<IEnumerable<SessionTemplate>> SearchTemplatesAsync(string searchTerm, SessionType? type = null)
    {
        var query = _context.SessionTemplates
            .Where(t => t.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(t =>
                t.Name.Contains(searchTerm) ||
                (t.Description != null && t.Description.Contains(searchTerm)));
        }

        if (type.HasValue)
        {
            query = query.Where(t => t.Type == type.Value);
        }

        return await query
            .OrderByDescending(t => t.UsageCount)
            .ToListAsync();
    }

    public async Task IncrementUsageCountAsync(int templateId)
    {
        var template = await _context.SessionTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.UsageCount++;
            template.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateLastUsedAtAsync(int templateId)
    {
        var template = await _context.SessionTemplates.FindAsync(templateId);
        if (template != null)
        {
            template.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<SessionTemplate>> GetPopularTemplatesAsync(int limit = 10)
    {
        return await _context.SessionTemplates
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.UsageCount)
            .ThenByDescending(t => t.LastUsedAt)
            .Take(limit)
            .ToListAsync();
    }
}
