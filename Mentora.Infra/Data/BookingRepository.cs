using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Data;

public class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _context;

    public BookingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Booking?> GetByIdAsync(string id)
    {
        return await _context.Bookings
            .Include(b => b.Session)
            .Include(b => b.Mentor)
            .Include(b => b.Mentee)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetByMentorIdAsync(string mentorId)
    {
        return await _context.Bookings
            .Where(b => b.MentorId == mentorId)
            .Include(b => b.Session)
            .Include(b => b.Mentee)
            .OrderBy(b => b.Session.StartAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetByMenteeIdAsync(string menteeId)
    {
        return await _context.Bookings
            .Where(b => b.MenteeId == menteeId)
            .Include(b => b.Session)
            .Include(b => b.Mentor)
            .OrderBy(b => b.Session.StartAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Booking>> GetBySessionIdAsync(string sessionId)
    {
        return await _context.Bookings
            .Where(b => b.SessionId == sessionId)
            .Include(b => b.Session)
            .Include(b => b.Mentor)
            .Include(b => b.Mentee)
            .ToListAsync();
    }

    public async Task<Booking> CreateAsync(Booking booking)
    {
        _context.Bookings.Add(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<Booking> UpdateAsync(Booking booking)
    {
        _context.Bookings.Update(booking);
        await _context.SaveChangesAsync();
        return booking;
    }

    public async Task<bool> DeleteAsync(string id)
    {
        var booking = await _context.Bookings.FindAsync(id);
        if (booking == null) return false;

        _context.Bookings.Remove(booking);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(string id)
    {
        return await _context.Bookings.AnyAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Booking>> GetBookingsByStatusAsync(SessionStatus status)
    {
        return await _context.Bookings
            .Where(b => b.Status == status)
            .Include(b => b.Session)
            .Include(b => b.Mentor)
            .Include(b => b.Mentee)
            .OrderBy(b => b.Session.StartAt)
            .ToListAsync();
    }
}