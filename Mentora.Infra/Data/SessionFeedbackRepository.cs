using Microsoft.EntityFrameworkCore;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;

namespace Mentora.Infra.Data;

public class SessionFeedbackRepository : ISessionFeedbackRepository
{
    private readonly ApplicationDbContext _context;

    public SessionFeedbackRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SessionFeedback?> GetByIdAsync(int id)
    {
        return await _context.SessionFeedbacks
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<SessionFeedback?> GetBySessionIdAndUserIdAsync(int sessionId, string userId)
    {
        return await _context.SessionFeedbacks
            .Include(f => f.Ratings)
            .FirstOrDefaultAsync(f => f.SessionId == sessionId.ToString() && f.UserId == userId);
    }

    public async Task<IEnumerable<SessionFeedback>> GetBySessionIdAsync(int sessionId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.SessionId == sessionId.ToString())
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetByUserIdAsync(string userId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.UserId == userId)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetByMentorIdAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentorId)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetPublicFeedbackByMentorIdAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentorId && f.IsPublic && !f.IsHidden)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetVerifiedFeedbackByMentorIdAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentorId && f.IsVerified && !f.IsHidden)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetFlaggedFeedbackAsync()
    {
        return await _context.SessionFeedbacks
            .Where(f => f.IsFlagged)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.FlaggedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<SessionFeedback>> GetHiddenFeedbackAsync()
    {
        return await _context.SessionFeedbacks
            .Where(f => f.IsHidden)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.UpdatedAt)
            .ToListAsync();
    }

    public async Task<SessionFeedback> CreateAsync(SessionFeedback feedback)
    {
        _context.SessionFeedbacks.Add(feedback);
        await _context.SaveChangesAsync();
        return feedback;
    }

    public async Task<SessionFeedback> UpdateAsync(SessionFeedback feedback)
    {
        _context.SessionFeedbacks.Update(feedback);
        await _context.SaveChangesAsync();
        return feedback;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var feedback = await _context.SessionFeedbacks.FindAsync(id);
        if (feedback == null) return false;

        _context.SessionFeedbacks.Remove(feedback);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.SessionFeedbacks.AnyAsync(f => f.Id == id);
    }

    public async Task<bool> UserHasFeedbackForSessionAsync(int sessionId, string userId)
    {
        return await _context.SessionFeedbacks
            .AnyAsync(f => f.SessionId == sessionId.ToString() && f.UserId == userId);
    }

    public async Task<IEnumerable<SessionFeedback>> GetFeedbackByRatingRangeAsync(int minRating, int maxRating)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.OverallRating >= minRating && f.OverallRating <= maxRating && f.IsPublic && !f.IsHidden)
            .Include(f => f.Ratings)
            .Include(f => f.Session)
            .OrderByDescending(f => f.OverallRating)
            .ThenByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<Dictionary<int, int>> GetRatingDistributionForMentorAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentorId && f.IsVerified && !f.IsHidden)
            .GroupBy(f => f.OverallRating)
            .ToDictionaryAsync(g => g.Key, g => g.Count());
    }

    public async Task<double> GetAverageRatingForMentorAsync(string mentorId)
    {
        var ratings = await _context.SessionFeedbacks
            .Where(f => f.MentorId == mentorId && f.IsVerified && !f.IsHidden)
            .ToListAsync();

        if (ratings.Count == 0) return 0;

        return ratings.Average(f => f.OverallRating);
    }

    public async Task<int> GetTotalFeedbackCountForMentorAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .CountAsync(f => f.MentorId == mentorId && f.IsVerified && !f.IsHidden);
    }

    public async Task<int> GetWouldRecommendCountForMentorAsync(string mentorId)
    {
        return await _context.SessionFeedbacks
            .CountAsync(f => f.MentorId == mentorId && f.IsVerified && !f.IsHidden && f.WouldRecommend);
    }
}

public class FeedbackRatingRepository : IFeedbackRatingRepository
{
    private readonly ApplicationDbContext _context;

    public FeedbackRatingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FeedbackRating?> GetByIdAsync(int id)
    {
        return await _context.FeedbackRatings
            .Include(fr => fr.Feedback)
            .FirstOrDefaultAsync(fr => fr.Id == id);
    }

    public async Task<IEnumerable<FeedbackRating>> GetByFeedbackIdAsync(int feedbackId)
    {
        return await _context.FeedbackRatings
            .Where(fr => fr.FeedbackId == feedbackId)
            .Include(fr => fr.Feedback)
            .OrderBy(fr => fr.CriteriaName)
            .ToListAsync();
    }

    public async Task<FeedbackRating> CreateAsync(FeedbackRating rating)
    {
        _context.FeedbackRatings.Add(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    public async Task<FeedbackRating> UpdateAsync(FeedbackRating rating)
    {
        _context.FeedbackRatings.Update(rating);
        await _context.SaveChangesAsync();
        return rating;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var rating = await _context.FeedbackRatings.FindAsync(id);
        if (rating == null) return false;

        _context.FeedbackRatings.Remove(rating);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteByFeedbackIdAsync(int feedbackId)
    {
        var ratings = await _context.FeedbackRatings
            .Where(fr => fr.FeedbackId == feedbackId)
            .ToListAsync();

        if (ratings.Count == 0) return false;

        _context.FeedbackRatings.RemoveRange(ratings);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.FeedbackRatings.AnyAsync(fr => fr.Id == id);
    }
}
