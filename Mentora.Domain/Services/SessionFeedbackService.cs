using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using AutoMapper;

namespace Mentora.Domain.Services;

public class SessionFeedbackService : ISessionFeedbackService
{
    private readonly ISessionFeedbackRepository _feedbackRepository;
    private readonly IFeedbackRatingRepository _ratingRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IMapper _mapper;

    public SessionFeedbackService(
        ISessionFeedbackRepository feedbackRepository,
        IFeedbackRatingRepository ratingRepository,
        ISessionRepository sessionRepository,
        IMapper mapper)
    {
        _feedbackRepository = feedbackRepository;
        _ratingRepository = ratingRepository;
        _sessionRepository = sessionRepository;
        _mapper = mapper;
    }

    public async Task<ResponseFeedbackDto> CreateFeedbackAsync(CreateFeedbackDto createDto, string userId)
    {
        // Validate that the user can leave feedback for this session
        if (!await CanUserLeaveFeedbackAsync(createDto.SessionId, userId))
        {
            throw new UnauthorizedAccessException("You cannot leave feedback for this session.");
        }

        // Check if user already left feedback
        if (await _feedbackRepository.UserHasFeedbackForSessionAsync(createDto.SessionId, userId))
        {
            throw new InvalidOperationException("You have already left feedback for this session.");
        }

        // Get session details
        var session = await _sessionRepository.GetByIdAsync(createDto.SessionId);
        if (session == null)
        {
            throw new ArgumentException("Session not found.");
        }

        var feedback = new SessionFeedback
        {
            SessionId = createDto.SessionId.ToString(),
            UserId = userId,
            MentorId = session.MentorId,
            OverallRating = createDto.OverallRating,
            ContentQualityRating = createDto.ContentQualityRating,
            MentorExpertiseRating = createDto.MentorExpertiseRating,
            CommunicationRating = createDto.CommunicationRating,
            ValueForMoneyRating = createDto.ValueForMoneyRating,
            OverallExperienceRating = createDto.OverallExperienceRating,
            WhatWentWell = createDto.WhatWentWell,
            WhatCouldBeImproved = createDto.WhatCouldBeImproved,
            AdditionalComments = createDto.AdditionalComments,
            IsPublic = createDto.IsPublic,
            WouldRecommend = createDto.WouldRecommend
        };

        var createdFeedback = await _feedbackRepository.CreateAsync(feedback);

        // Create individual rating records for detailed criteria
        var ratings = new List<FeedbackRating>();
        if (createDto.ContentQualityRating.HasValue)
        {
            ratings.Add(new FeedbackRating
            {
                FeedbackId = createdFeedback.Id,
                CriteriaName = "Content Quality",
                Rating = createDto.ContentQualityRating.Value
            });
        }
        if (createDto.MentorExpertiseRating.HasValue)
        {
            ratings.Add(new FeedbackRating
            {
                FeedbackId = createdFeedback.Id,
                CriteriaName = "Mentor Expertise",
                Rating = createDto.MentorExpertiseRating.Value
            });
        }
        if (createDto.CommunicationRating.HasValue)
        {
            ratings.Add(new FeedbackRating
            {
                FeedbackId = createdFeedback.Id,
                CriteriaName = "Communication",
                Rating = createDto.CommunicationRating.Value
            });
        }
        if (createDto.ValueForMoneyRating.HasValue)
        {
            ratings.Add(new FeedbackRating
            {
                FeedbackId = createdFeedback.Id,
                CriteriaName = "Value for Money",
                Rating = createDto.ValueForMoneyRating.Value
            });
        }
        if (createDto.OverallExperienceRating.HasValue)
        {
            ratings.Add(new FeedbackRating
            {
                FeedbackId = createdFeedback.Id,
                CriteriaName = "Overall Experience",
                Rating = createDto.OverallExperienceRating.Value
            });
        }

        foreach (var rating in ratings)
        {
            await _ratingRepository.CreateAsync(rating);
        }

        // Reload feedback with ratings
        var feedbackWithRatings = await _feedbackRepository.GetByIdAsync(createdFeedback.Id);
        return _mapper.Map<ResponseFeedbackDto>(feedbackWithRatings);
    }

    public async Task<ResponseFeedbackDto?> GetFeedbackByIdAsync(int id, string userId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(id);
        if (feedback == null) return null;

        // Check access permissions
        if (!await HasFeedbackAccessAsync(feedback, userId))
        {
            throw new UnauthorizedAccessException("You do not have permission to view this feedback.");
        }

        return _mapper.Map<ResponseFeedbackDto>(feedback);
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetFeedbackBySessionIdAsync(int sessionId, string userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException("Session not found.");
        }

        var feedbacks = await _feedbackRepository.GetBySessionIdAsync(sessionId);

        // Filter based on access permissions
        var accessibleFeedbacks = new List<SessionFeedback>();
        foreach (var feedback in feedbacks)
        {
            if (await HasFeedbackAccessAsync(feedback, userId))
            {
                accessibleFeedbacks.Add(feedback);
            }
        }

        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(accessibleFeedbacks);
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetUserFeedbackAsync(string userId)
    {
        var feedbacks = await _feedbackRepository.GetByUserIdAsync(userId);
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<ResponseFeedbackDto?> UpdateFeedbackAsync(int id, UpdateFeedbackDto updateDto, string userId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(id);
        if (feedback == null) return null;

        if (!IsFeedbackOwnerAsync(id, userId).Result)
        {
            throw new UnauthorizedAccessException("You can only update your own feedback.");
        }

        // Only allow updates within 24 hours of creation
        if (feedback.CreatedAt.AddHours(24) < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Feedback can only be updated within 24 hours of submission.");
        }

        _mapper.Map(updateDto, feedback);
        feedback.UpdatedAt = DateTime.UtcNow;

        var updatedFeedback = await _feedbackRepository.UpdateAsync(feedback);
        return _mapper.Map<ResponseFeedbackDto>(updatedFeedback);
    }

    public async Task<bool> DeleteFeedbackAsync(int id, string userId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(id);
        if (feedback == null) return false;

        if (!IsFeedbackOwnerAsync(id, userId).Result && !IsAdminUser(userId))
        {
            throw new UnauthorizedAccessException("You can only delete your own feedback.");
        }

        // Only allow deletion within 24 hours of creation
        if (feedback.CreatedAt.AddHours(24) < DateTime.UtcNow && !IsAdminUser(userId))
        {
            throw new InvalidOperationException("Feedback can only be deleted within 24 hours of submission.");
        }

        await _ratingRepository.DeleteByFeedbackIdAsync(id);
        return await _feedbackRepository.DeleteAsync(id);
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetMentorFeedbackAsync(string mentorId, string requestingUserId)
    {
        // Check if requesting user is the mentor or an admin
        if (requestingUserId != mentorId && !IsAdminUser(requestingUserId))
        {
            throw new UnauthorizedAccessException("You can only view your own feedback unless you are an admin.");
        }

        var feedbacks = await _feedbackRepository.GetByMentorIdAsync(mentorId);
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetPublicMentorFeedbackAsync(string mentorId)
    {
        var feedbacks = await _feedbackRepository.GetPublicFeedbackByMentorIdAsync(mentorId);
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<bool> RespondToFeedbackAsync(int feedbackId, MentorFeedbackResponseDto responseDto, string mentorId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null) return false;

        if (!IsFeedbackMentorAsync(feedbackId, mentorId).Result)
        {
            throw new UnauthorizedAccessException("Only the session mentor can respond to feedback.");
        }

        feedback.AddMentorResponse(responseDto.Response);
        await _feedbackRepository.UpdateAsync(feedback);
        return true;
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetFlaggedFeedbackAsync()
    {
        var feedbacks = await _feedbackRepository.GetFlaggedFeedbackAsync();
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetHiddenFeedbackAsync()
    {
        var feedbacks = await _feedbackRepository.GetHiddenFeedbackAsync();
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<bool> ModerateFeedbackAsync(int feedbackId, FeedbackModerationDto moderationDto)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null) return false;

        feedback.IsHidden = moderationDto.IsHidden;
        feedback.UpdatedAt = DateTime.UtcNow;

        await _feedbackRepository.UpdateAsync(feedback);
        return true;
    }

    public async Task<bool> BulkModerateFeedbackAsync(BulkFeedbackActionDto bulkActionDto)
    {
        var success = true;
        foreach (var feedbackId in bulkActionDto.FeedbackIds)
        {
            try
            {
                var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
                if (feedback != null)
                {
                    feedback.IsHidden = bulkActionDto.IsHidden;
                    feedback.UpdatedAt = DateTime.UtcNow;
                    await _feedbackRepository.UpdateAsync(feedback);
                }
            }
            catch
            {
                success = false;
            }
        }
        return success;
    }

    public async Task<bool> FlagFeedbackAsync(int feedbackId, FeedbackFlagDto flagDto, string userId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        if (feedback == null) return false;

        feedback.FlagFeedback(flagDto.Reason);
        await _feedbackRepository.UpdateAsync(feedback);
        return true;
    }

    public async Task<FeedbackStatsDto> GetFeedbackStatsAsync(string userId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var allFeedbacks = await _feedbackRepository.GetByUserIdAsync(userId);

        var filteredFeedbacks = allFeedbacks.Where(f =>
            (!startDate.HasValue || f.CreatedAt >= startDate.Value) &&
            (!endDate.HasValue || f.CreatedAt <= endDate.Value)).ToList();

        var stats = new FeedbackStatsDto
        {
            TotalFeedback = filteredFeedbacks.Count,
            PublicFeedback = filteredFeedbacks.Count(f => f.IsPublic),
            VerifiedFeedback = filteredFeedbacks.Count(f => f.IsVerified),
            FlaggedFeedback = filteredFeedbacks.Count(f => f.IsFlagged),
            HiddenFeedback = filteredFeedbacks.Count(f => f.IsHidden),
            WouldRecommendCount = filteredFeedbacks.Count(f => f.WouldRecommend)
        };

        if (stats.TotalFeedback > 0)
        {
            stats.AverageRating = filteredFeedbacks.Average(f => f.OverallRating);
            stats.RecommendationRate = (double)stats.WouldRecommendCount / stats.TotalFeedback * 100;
        }

        // Rating distribution
        stats.RatingDistribution = filteredFeedbacks
            .GroupBy(f => f.OverallRating)
            .ToDictionary(g => g.Key, g => g.Count());

        // Monthly stats
        stats.MonthlyStats = filteredFeedbacks
            .GroupBy(f => f.CreatedAt.ToString("yyyy-MM"))
            .Select(g => new MonthlyFeedbackStatsDto
            {
                Month = g.Key,
                TotalFeedback = g.Count(),
                AverageRating = g.Average(f => f.OverallRating),
                WouldRecommendCount = g.Count(f => f.WouldRecommend)
            })
            .ToList();

        foreach (var monthly in stats.MonthlyStats)
        {
            monthly.RecommendationRate = monthly.TotalFeedback > 0 ?
                (double)monthly.WouldRecommendCount / monthly.TotalFeedback * 100 : 0;
        }

        return stats;
    }

    public async Task<MentorRatingSummaryDto> GetMentorRatingSummaryAsync(string mentorId)
    {
        var ratingDistribution = await _feedbackRepository.GetRatingDistributionForMentorAsync(mentorId);
        var averageRating = await _feedbackRepository.GetAverageRatingForMentorAsync(mentorId);
        var totalCount = await _feedbackRepository.GetTotalFeedbackCountForMentorAsync(mentorId);
        var wouldRecommendCount = await _feedbackRepository.GetWouldRecommendCountForMentorAsync(mentorId);

        var verifiedFeedbacks = await _feedbackRepository.GetVerifiedFeedbackByMentorIdAsync(mentorId);
        var lastFeedbackAt = verifiedFeedbacks.FirstOrDefault()?.CreatedAt;

        var criteriaStats = new List<CriteriaRatingStatsDto>
        {
            new CriteriaRatingStatsDto
            {
                CriteriaName = "Content Quality",
                AverageRating = verifiedFeedbacks.Where(f => f.ContentQualityRating.HasValue)
                    .Average(f => f.ContentQualityRating!.Value),
                TotalRatings = verifiedFeedbacks.Count(f => f.ContentQualityRating.HasValue),
                RatingDistribution = verifiedFeedbacks
                    .Where(f => f.ContentQualityRating.HasValue)
                    .GroupBy(f => f.ContentQualityRating!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            new CriteriaRatingStatsDto
            {
                CriteriaName = "Mentor Expertise",
                AverageRating = verifiedFeedbacks.Where(f => f.MentorExpertiseRating.HasValue)
                    .Average(f => f.MentorExpertiseRating!.Value),
                TotalRatings = verifiedFeedbacks.Count(f => f.MentorExpertiseRating.HasValue),
                RatingDistribution = verifiedFeedbacks
                    .Where(f => f.MentorExpertiseRating.HasValue)
                    .GroupBy(f => f.MentorExpertiseRating!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            new CriteriaRatingStatsDto
            {
                CriteriaName = "Communication",
                AverageRating = verifiedFeedbacks.Where(f => f.CommunicationRating.HasValue)
                    .Average(f => f.CommunicationRating!.Value),
                TotalRatings = verifiedFeedbacks.Count(f => f.CommunicationRating.HasValue),
                RatingDistribution = verifiedFeedbacks
                    .Where(f => f.CommunicationRating.HasValue)
                    .GroupBy(f => f.CommunicationRating!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            new CriteriaRatingStatsDto
            {
                CriteriaName = "Value for Money",
                AverageRating = verifiedFeedbacks.Where(f => f.ValueForMoneyRating.HasValue)
                    .Average(f => f.ValueForMoneyRating!.Value),
                TotalRatings = verifiedFeedbacks.Count(f => f.ValueForMoneyRating.HasValue),
                RatingDistribution = verifiedFeedbacks
                    .Where(f => f.ValueForMoneyRating.HasValue)
                    .GroupBy(f => f.ValueForMoneyRating!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            },
            new CriteriaRatingStatsDto
            {
                CriteriaName = "Overall Experience",
                AverageRating = verifiedFeedbacks.Where(f => f.OverallExperienceRating.HasValue)
                    .Average(f => f.OverallExperienceRating!.Value),
                TotalRatings = verifiedFeedbacks.Count(f => f.OverallExperienceRating.HasValue),
                RatingDistribution = verifiedFeedbacks
                    .Where(f => f.OverallExperienceRating.HasValue)
                    .GroupBy(f => f.OverallExperienceRating!.Value)
                    .ToDictionary(g => g.Key, g => g.Count())
            }
        };

        return new MentorRatingSummaryDto
        {
            MentorId = mentorId,
            MentorName = $"Mentor {mentorId}", // This would typically fetch from user service
            TotalSessions = 0, // This would require additional query
            TotalFeedback = totalCount,
            AverageRating = averageRating,
            WouldRecommendCount = wouldRecommendCount,
            RecommendationRate = totalCount > 0 ? (double)wouldRecommendCount / totalCount * 100 : 0,
            CriteriaStats = criteriaStats,
            LastFeedbackAt = lastFeedbackAt
        };
    }

    public async Task<IEnumerable<MentorRatingSummaryDto>> GetTopRatedMentorsAsync(int count = 10)
    {
        // This would typically involve a more complex query to get top-rated mentors
        // For now, return empty list - implementation would be added in a full system
        return new List<MentorRatingSummaryDto>();
    }

    public async Task<IEnumerable<ResponseFeedbackDto>> GetFeedbackByRatingRangeAsync(int minRating, int maxRating)
    {
        var feedbacks = await _feedbackRepository.GetFeedbackByRatingRangeAsync(minRating, maxRating);
        return _mapper.Map<IEnumerable<ResponseFeedbackDto>>(feedbacks);
    }

    public async Task<bool> CanUserLeaveFeedbackAsync(int sessionId, string userId)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId);
        if (session == null) return false;

        // User must be a participant (mentor or mentee) in the session
        // This would typically involve checking bookings or session participants
        // For now, assume any user can leave feedback if session exists and they haven't already
        return true;
    }

    public async Task<bool> IsFeedbackOwnerAsync(int feedbackId, string userId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        return feedback?.UserId == userId;
    }

    public async Task<bool> IsFeedbackMentorAsync(int feedbackId, string mentorId)
    {
        var feedback = await _feedbackRepository.GetByIdAsync(feedbackId);
        return feedback?.MentorId == mentorId;
    }

    private async Task<bool> HasFeedbackAccessAsync(SessionFeedback feedback, string userId)
    {
        // Owner can always access their feedback
        if (feedback.UserId == userId) return true;

        // Mentor can access feedback about themselves
        if (feedback.MentorId == userId) return true;

        // Public feedback can be accessed by anyone
        if (feedback.IsPublic && !feedback.IsHidden) return true;

        // Admins can access all feedback
        if (IsAdminUser(userId)) return true;

        return false;
    }

    private bool IsAdminUser(string userId)
    {
        // This would typically check against user roles or admin claims
        // For now, return false - would need actual implementation
        return false;
    }
}
