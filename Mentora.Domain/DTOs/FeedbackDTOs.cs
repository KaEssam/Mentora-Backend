using System.ComponentModel.DataAnnotations;
using Mentora.Core.Data;

namespace Mentora.Domain.DTOs;

public class CreateFeedbackDto
{
    [Required]
    public int SessionId { get; set; }

    [Required]
    [Range(1, 5)]
    public int OverallRating { get; set; }

    [Range(1, 5)]
    public int? ContentQualityRating { get; set; }

    [Range(1, 5)]
    public int? MentorExpertiseRating { get; set; }

    [Range(1, 5)]
    public int? CommunicationRating { get; set; }

    [Range(1, 5)]
    public int? ValueForMoneyRating { get; set; }

    [Range(1, 5)]
    public int? OverallExperienceRating { get; set; }

    [StringLength(2000)]
    public string? WhatWentWell { get; set; }

    [StringLength(2000)]
    public string? WhatCouldBeImproved { get; set; }

    [StringLength(2000)]
    public string? AdditionalComments { get; set; }

    public bool IsPublic { get; set; } = false;
    public bool WouldRecommend { get; set; } = false;
}

public class UpdateFeedbackDto
{
    [Range(1, 5)]
    public int? OverallRating { get; set; }

    [Range(1, 5)]
    public int? ContentQualityRating { get; set; }

    [Range(1, 5)]
    public int? MentorExpertiseRating { get; set; }

    [Range(1, 5)]
    public int? CommunicationRating { get; set; }

    [Range(1, 5)]
    public int? ValueForMoneyRating { get; set; }

    [Range(1, 5)]
    public int? OverallExperienceRating { get; set; }

    [StringLength(2000)]
    public string? WhatWentWell { get; set; }

    [StringLength(2000)]
    public string? WhatCouldBeImproved { get; set; }

    [StringLength(2000)]
    public string? AdditionalComments { get; set; }

    public bool? IsPublic { get; set; }
    public bool? WouldRecommend { get; set; }
}

public class ResponseFeedbackDto
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string MentorId { get; set; } = string.Empty;
    public int OverallRating { get; set; }
    public int? ContentQualityRating { get; set; }
    public int? MentorExpertiseRating { get; set; }
    public int? CommunicationRating { get; set; }
    public int? ValueForMoneyRating { get; set; }
    public int? OverallExperienceRating { get; set; }
    public double AverageRating { get; set; }
    public string? WhatWentWell { get; set; }
    public string? WhatCouldBeImproved { get; set; }
    public string? AdditionalComments { get; set; }
    public bool IsPublic { get; set; }
    public bool IsVerified { get; set; }
    public bool WouldRecommend { get; set; }
    public string? MentorResponse { get; set; }
    public DateTime? MentorResponseAt { get; set; }
    public bool IsFlagged { get; set; }
    public string? FlaggedReason { get; set; }
    public DateTime? FlaggedAt { get; set; }
    public bool IsHidden { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<FeedbackRatingDto> Ratings { get; set; } = new();
}

public class FeedbackRatingDto
{
    public int Id { get; set; }
    public int FeedbackId { get; set; }
    public string CriteriaName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MentorFeedbackResponseDto
{
    [StringLength(2000)]
    public string Response { get; set; } = string.Empty;
}

public class FeedbackFlagDto
{
    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class FeedbackStatsDto
{
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int PublicFeedback { get; set; }
    public int VerifiedFeedback { get; set; }
    public int FlaggedFeedback { get; set; }
    public int HiddenFeedback { get; set; }
    public int WouldRecommendCount { get; set; }
    public double RecommendationRate { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // Rating -> Count
    public List<MonthlyFeedbackStatsDto> MonthlyStats { get; set; } = new();
    public List<CriteriaRatingStatsDto> CriteriaStats { get; set; } = new();
}

public class MonthlyFeedbackStatsDto
{
    public string Month { get; set; } = string.Empty; // Format: "2024-01"
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int WouldRecommendCount { get; set; }
    public double RecommendationRate { get; set; }
}

public class CriteriaRatingStatsDto
{
    public string CriteriaName { get; set; } = string.Empty;
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public Dictionary<int, int> RatingDistribution { get; set; } = new(); // Rating -> Count
}

public class MentorRatingSummaryDto
{
    public string MentorId { get; set; } = string.Empty;
    public string MentorName { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    public int TotalFeedback { get; set; }
    public double AverageRating { get; set; }
    public int WouldRecommendCount { get; set; }
    public double RecommendationRate { get; set; }
    public List<CriteriaRatingStatsDto> CriteriaStats { get; set; } = new();
    public DateTime? LastFeedbackAt { get; set; }
}

public class FeedbackModerationDto
{
    public int FeedbackId { get; set; }
    public bool IsHidden { get; set; }
    public string? ModerationNote { get; set; }
}

public class BulkFeedbackActionDto
{
    [Required]
    public List<int> FeedbackIds { get; set; } = new();

    public bool IsHidden { get; set; }
    public bool IsVerified { get; set; }
    public bool IsFlagged { get; set; }
    public string? ModerationNote { get; set; }
}
