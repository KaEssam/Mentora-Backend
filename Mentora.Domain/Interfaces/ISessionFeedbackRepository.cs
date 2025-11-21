using Mentora.Core.Data;

namespace Mentora.Domain.Interfaces;

public interface ISessionFeedbackRepository
{
    Task<SessionFeedback?> GetByIdAsync(int id);
    Task<SessionFeedback?> GetBySessionIdAndUserIdAsync(int sessionId, string userId);
    Task<IEnumerable<SessionFeedback>> GetBySessionIdAsync(int sessionId);
    Task<IEnumerable<SessionFeedback>> GetByUserIdAsync(string userId);
    Task<IEnumerable<SessionFeedback>> GetByMentorIdAsync(string mentorId);
    Task<IEnumerable<SessionFeedback>> GetPublicFeedbackByMentorIdAsync(string mentorId);
    Task<IEnumerable<SessionFeedback>> GetVerifiedFeedbackByMentorIdAsync(string mentorId);
    Task<IEnumerable<SessionFeedback>> GetFlaggedFeedbackAsync();
    Task<IEnumerable<SessionFeedback>> GetHiddenFeedbackAsync();
    Task<SessionFeedback> CreateAsync(SessionFeedback feedback);
    Task<SessionFeedback> UpdateAsync(SessionFeedback feedback);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<bool> UserHasFeedbackForSessionAsync(int sessionId, string userId);
    Task<IEnumerable<SessionFeedback>> GetFeedbackByRatingRangeAsync(int minRating, int maxRating);
    Task<Dictionary<int, int>> GetRatingDistributionForMentorAsync(string mentorId);
    Task<double> GetAverageRatingForMentorAsync(string mentorId);
    Task<int> GetTotalFeedbackCountForMentorAsync(string mentorId);
    Task<int> GetWouldRecommendCountForMentorAsync(string mentorId);
}

public interface IFeedbackRatingRepository
{
    Task<FeedbackRating?> GetByIdAsync(int id);
    Task<IEnumerable<FeedbackRating>> GetByFeedbackIdAsync(int feedbackId);
    Task<FeedbackRating> CreateAsync(FeedbackRating rating);
    Task<FeedbackRating> UpdateAsync(FeedbackRating rating);
    Task<bool> DeleteAsync(int id);
    Task<bool> DeleteByFeedbackIdAsync(int feedbackId);
    Task<bool> ExistsAsync(int id);
}
