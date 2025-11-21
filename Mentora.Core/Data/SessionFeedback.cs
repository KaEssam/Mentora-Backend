using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Mentora.Core.Data;

public class SessionFeedback
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(450)]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(450)]
    public string MentorId { get; set; } = string.Empty;

    // Overall rating (1-5 stars)
    [Required]
    [Range(1, 5)]
    public int OverallRating { get; set; }

    // Individual rating criteria
    public int? ContentQualityRating { get; set; }      // 1-5
    public int? MentorExpertiseRating { get; set; }     // 1-5
    public int? CommunicationRating { get; set; }       // 1-5
    public int? ValueForMoneyRating { get; set; }       // 1-5
    public int? OverallExperienceRating { get; set; }   // 1-5

    // Feedback text
    [StringLength(2000)]
    public string? WhatWentWell { get; set; }

    [StringLength(2000)]
    public string? WhatCouldBeImproved { get; set; }

    [StringLength(2000)]
    public string? AdditionalComments { get; set; }

    // Feedback metadata
    public bool IsPublic { get; set; } = false;
    public bool IsVerified { get; set; } = false;
    public bool WouldRecommend { get; set; } = false;

    // Response from mentor
    [StringLength(2000)]
    public string? MentorResponse { get; set; }

    public DateTime? MentorResponseAt { get; set; }

    // Moderation
    public bool IsFlagged { get; set; } = false;
    public string? FlaggedReason { get; set; }
    public DateTime? FlaggedAt { get; set; }
    public bool IsHidden { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Session Session { get; set; } = null!;
    public virtual ICollection<FeedbackRating> Ratings { get; set; } = new List<FeedbackRating>();

    // Helper methods
    public double GetAverageRating()
    {
        var ratings = new List<int?>
        {
            ContentQualityRating,
            MentorExpertiseRating,
            CommunicationRating,
            ValueForMoneyRating,
            OverallExperienceRating
        }.Where(r => r.HasValue).Select(r => r!.Value).ToList();

        if (ratings.Count == 0) return OverallRating;

        return (ratings.Sum() + OverallRating) / (ratings.Count + 1);
    }

    public bool HasDetailedRatings()
    {
        return ContentQualityRating.HasValue ||
               MentorExpertiseRating.HasValue ||
               CommunicationRating.HasValue ||
               ValueForMoneyRating.HasValue ||
               OverallExperienceRating.HasValue;
    }

    public void MarkAsVerified()
    {
        IsVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddMentorResponse(string response)
    {
        MentorResponse = response;
        MentorResponseAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FlagFeedback(string reason)
    {
        IsFlagged = true;
        FlaggedReason = reason;
        FlaggedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}

public class FeedbackRating
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int FeedbackId { get; set; }

    [Required]
    [StringLength(100)]
    public string CriteriaName { get; set; } = string.Empty;

    [Required]
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(500)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual SessionFeedback Feedback { get; set; } = null!;
}

public enum FeedbackType
{
    SessionRating = 1,
    MentorReview = 2,
    PlatformFeedback = 3
}
