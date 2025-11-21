using Mentora.Core.Data;
using Mentora.Domain.DTOs;

namespace Mentora.Domain.Interfaces;

public interface ISessionFeedbackService
{
    // Feedback CRUD operations
    Task<ResponseFeedbackDto> CreateFeedbackAsync(CreateFeedbackDto createDto, string userId);
    Task<ResponseFeedbackDto?> GetFeedbackByIdAsync(int id, string userId);
    Task<IEnumerable<ResponseFeedbackDto>> GetFeedbackBySessionIdAsync(int sessionId, string userId);
    Task<IEnumerable<ResponseFeedbackDto>> GetUserFeedbackAsync(string userId);
    Task<ResponseFeedbackDto?> UpdateFeedbackAsync(int id, UpdateFeedbackDto updateDto, string userId);
    Task<bool> DeleteFeedbackAsync(int id, string userId);

    // Mentor feedback operations
    Task<IEnumerable<ResponseFeedbackDto>> GetMentorFeedbackAsync(string mentorId, string requestingUserId);
    Task<IEnumerable<ResponseFeedbackDto>> GetPublicMentorFeedbackAsync(string mentorId);
    Task<bool> RespondToFeedbackAsync(int feedbackId, MentorFeedbackResponseDto responseDto, string mentorId);

    // Admin moderation operations
    Task<IEnumerable<ResponseFeedbackDto>> GetFlaggedFeedbackAsync();
    Task<IEnumerable<ResponseFeedbackDto>> GetHiddenFeedbackAsync();
    Task<bool> ModerateFeedbackAsync(int feedbackId, FeedbackModerationDto moderationDto);
    Task<bool> BulkModerateFeedbackAsync(BulkFeedbackActionDto bulkActionDto);
    Task<bool> FlagFeedbackAsync(int feedbackId, FeedbackFlagDto flagDto, string userId);

    // Rating and statistics operations
    Task<FeedbackStatsDto> GetFeedbackStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null);
    Task<MentorRatingSummaryDto> GetMentorRatingSummaryAsync(string mentorId);
    Task<IEnumerable<MentorRatingSummaryDto>> GetTopRatedMentorsAsync(int count = 10);
    Task<IEnumerable<ResponseFeedbackDto>> GetFeedbackByRatingRangeAsync(int minRating, int maxRating);

    // Feedback validation
    Task<bool> CanUserLeaveFeedbackAsync(int sessionId, string userId);
    Task<bool> IsFeedbackOwnerAsync(int feedbackId, string userId);
    Task<bool> IsFeedbackMentorAsync(int feedbackId, string mentorId);
}
