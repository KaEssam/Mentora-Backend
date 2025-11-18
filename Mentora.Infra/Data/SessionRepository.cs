using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Data;

public class SessionRepository : ISessionRepository
{
    private readonly ApplicationDbContext _context;

    public SessionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Session?> GetByIdAsync(int id)
    {
        return await _context.Sessions
            .Include(s => s.Bookings)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Session>> GetByMentorIdAsync(string mentorId)
    {
        return await _context.Sessions
            .Where(s => s.MentorId == mentorId)
            .Include(s => s.Bookings)
            .OrderBy(s => s.StartAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Session>> GetAvailableSessionsAsync()
    {
        return await _context.Sessions
            .Where(s => s.Status == SessionStatus.Scheduled && s.StartAt > DateTime.UtcNow)
            .Include(s => s.Bookings)
            .OrderBy(s => s.StartAt)
            .ToListAsync();
    }

    public async Task<Session> CreateAsync(Session session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<Session> UpdateAsync(Session session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
        return session;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var session = await _context.Sessions.FindAsync(id);
        if (session == null) return false;

        _context.Sessions.Remove(session);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Sessions.AnyAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Session>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Sessions
            .Where(s => s.StartAt >= startDate && s.EndAt <= endDate)
            .Include(s => s.Bookings)
            .OrderBy(s => s.StartAt)
            .ToListAsync();
    }
}