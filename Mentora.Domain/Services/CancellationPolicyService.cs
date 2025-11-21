using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces.Repositories;

namespace Mentora.Domain.Services
{
    public interface ICancellationPolicyService
    {
        Task<CancellationPolicyResult> EvaluateCancellationPolicyAsync(int bookingId, string userId, string reason);
        Task<RefundCalculation> CalculateRefundAsync(Booking booking, CancellationPolicy policy);
        Task<List<CancellationPolicy>> GetAvailablePoliciesAsync();
        Task<CancellationPolicy> GetDefaultPolicyAsync();
        Task<CancellationPolicy> GetPolicyForBookingAsync(Booking booking);
        Task<bool> CanCancelBookingAsync(Booking booking, string userId);
        Task<CancellationPenalty> CalculatePenaltyAsync(Booking booking, CancellationPolicy policy);
        Task<BookingModificationResult> ValidateBookingModificationAsync(int bookingId, string userId, UpdateBookingDto updateDto);
    }

    public class CancellationPolicyService : ICancellationPolicyService
    {
        private readonly IBookingRepository _bookingRepository;

        public CancellationPolicyService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task<CancellationPolicyResult> EvaluateCancellationPolicyAsync(int bookingId, string userId, string reason)
        {
            var result = new CancellationPolicyResult();

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    result.IsAllowed = false;
                    result.Reason = "Booking not found";
                    return result;
                }

                // Check if user can cancel this booking
                if (!await CanCancelBookingAsync(booking, userId))
                {
                    result.IsAllowed = false;
                    result.Reason = "You are not authorized to cancel this booking or the booking cannot be cancelled at this time";
                    return result;
                }

                // Get applicable cancellation policy
                var policy = await GetPolicyForBookingAsync(booking);

                // Calculate refund and penalties
                var refundCalculation = await CalculateRefundAsync(booking, policy);
                var penalty = await CalculatePenaltyAsync(booking, policy);

                result.IsAllowed = true;
                result.Policy = policy;
                result.RefundCalculation = refundCalculation;
                result.Penalty = penalty;
                result.ProcessingFee = CalculateProcessingFee(booking.Amount);
                result.EstimatedRefundAmount = refundCalculation.RefundAmount - penalty.PenaltyAmount - result.ProcessingFee;

                // Check for special circumstances
                if (HasSpecialCircumstances(reason))
                {
                    result.HasSpecialCircumstances = true;
                    result.Reason = "Special circumstances detected - may be eligible for full refund";
                    result.EstimatedRefundAmount = booking.Amount; // Full refund for special circumstances
                }
            }
            catch (Exception ex)
            {
                result.IsAllowed = false;
                result.Reason = $"Error evaluating cancellation policy: {ex.Message}";
            }

            return result;
        }

        public async Task<RefundCalculation> CalculateRefundAsync(Booking booking, CancellationPolicy policy)
        {
            var calculation = new RefundCalculation
            {
                OriginalAmount = booking.Amount,
                Currency = booking.Currency
            };

            var timeUntilSession = booking.SessionStartTime - DateTime.UtcNow;
            var hoursUntilSession = timeUntilSession.TotalHours;

            // Apply refund tiers based on policy
            foreach (var tier in policy.RefundTiers.OrderByDescending(t => t.HoursBeforeSession))
            {
                if (hoursUntilSession >= tier.HoursBeforeSession)
                {
                    calculation.RefundPercentage = tier.RefundPercentage;
                    calculation.RefundAmount = booking.Amount * (tier.RefundPercentage / 100.0m);
                    calculation.AppliedTier = tier;
                    break;
                }
            }

            // If no tier matches, apply minimum refund
            if (calculation.AppliedTier == null)
            {
                calculation.RefundPercentage = policy.MinimumRefundPercentage;
                calculation.RefundAmount = booking.Amount * (policy.MinimumRefundPercentage / 100.0m);
                calculation.AppliedTier = new RefundTier
                {
                    HoursBeforeSession = 0,
                    RefundPercentage = policy.MinimumRefundPercentage,
                    Description = "Minimum refund"
                };
            }

            return calculation;
        }

        public async Task<List<CancellationPolicy>> GetAvailablePoliciesAsync()
        {
            return new List<CancellationPolicy>
            {
                // Standard Policy
                new CancellationPolicy
                {
                    Id = 1,
                    Name = "Standard Policy",
                    Description = "Standard cancellation policy with tiered refunds",
                    IsActive = true,
                    MinimumRefundPercentage = 25,
                    ProcessingFeePercentage = 5,
                    RefundTiers = new List<RefundTier>
                    {
                        new RefundTier { HoursBeforeSession = 168, RefundPercentage = 100, Description = "7+ days before session" }, // 7 days
                        new RefundTier { HoursBeforeSession = 72, RefundPercentage = 75, Description = "3-7 days before session" },   // 3 days
                        new RefundTier { HoursBeforeSession = 24, RefundPercentage = 50, Description = "1-3 days before session" },    // 1 day
                        new RefundTier { HoursBeforeSession = 2, RefundPercentage = 25, Description = "2-24 hours before session" }   // 2 hours
                    }
                },
                // Flexible Policy
                new CancellationPolicy
                {
                    Id = 2,
                    Name = "Flexible Policy",
                    Description = "More lenient cancellation terms",
                    IsActive = true,
                    MinimumRefundPercentage = 50,
                    ProcessingFeePercentage = 3,
                    RefundTiers = new List<RefundTier>
                    {
                        new RefundTier { HoursBeforeSession = 96, RefundPercentage = 100, Description = "4+ days before session" },   // 4 days
                        new RefundTier { HoursBeforeSession = 48, RefundPercentage = 90, Description = "2-4 days before session" },   // 2 days
                        new RefundTier { HoursBeforeSession = 24, RefundPercentage = 75, Description = "1-2 days before session" },   // 1 day
                        new RefundTier { HoursBeforeSession = 4, RefundPercentage = 50, Description = "4-24 hours before session" }  // 4 hours
                    }
                },
                // Strict Policy
                new CancellationPolicy
                {
                    Id = 3,
                    Name = "Strict Policy",
                    Description = "Strict cancellation terms for premium sessions",
                    IsActive = true,
                    MinimumRefundPercentage = 10,
                    ProcessingFeePercentage = 10,
                    RefundTiers = new List<RefundTier>
                    {
                        new RefundTier { HoursBeforeSession = 168, RefundPercentage = 75, Description = "7+ days before session" },  // 7 days
                        new RefundTier { HoursBeforeSession = 96, RefundPercentage = 50, Description = "4-7 days before session" },  // 4 days
                        new RefundTier { HoursBeforeSession = 48, RefundPercentage = 25, Description = "2-4 days before session" },  // 2 days
                        new RefundTier { HoursBeforeSession = 24, RefundPercentage = 10, Description = "1-2 days before session" }  // 1 day
                    }
                }
            };
        }

        public async Task<CancellationPolicy> GetDefaultPolicyAsync()
        {
            var policies = await GetAvailablePoliciesAsync();
            return policies.FirstOrDefault(p => p.Name == "Standard Policy") ?? policies.First();
        }

        public async Task<CancellationPolicy> GetPolicyForBookingAsync(Booking booking)
        {
            // For now, return the standard policy
            // In a real implementation, this could be based on:
            // - Session type
            // - Mentor preferences
            // - User subscription level
            // - Special promotions
            return await GetDefaultPolicyAsync();
        }

        public async Task<bool> CanCancelBookingAsync(Booking booking, string userId)
        {
            // Check if user owns this booking
            if (booking.UserId != userId && booking.MentorId != userId)
                return false;

            // Check if booking can be cancelled
            if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                return false;

            // Check if it's too late to cancel (less than 30 minutes before session)
            if (booking.SessionStartTime <= DateTime.UtcNow.AddMinutes(30))
                return false;

            // Check if payment has been processed (can't cancel unpaid bookings after certain time)
            if (!booking.IsPaid && booking.BookingDate <= DateTime.UtcNow.AddHours(-24))
                return false;

            return true;
        }

        public async Task<CancellationPenalty> CalculatePenaltyAsync(Booking booking, CancellationPolicy policy)
        {
            var penalty = new CancellationPenalty();
            var timeUntilSession = booking.SessionStartTime - DateTime.UtcNow;
            var hoursUntilSession = timeUntilSession.TotalHours;

            // Calculate penalty based on how close to session time
            if (hoursUntilSession < 24)
            {
                penalty.PenaltyType = PenaltyType.LateCancellation;
                penalty.PenaltyPercentage = 25; // 25% penalty for cancellations within 24 hours
                penalty.Description = "Late cancellation penalty (within 24 hours of session)";
            }
            else if (hoursUntilSession < 72)
            {
                penalty.PenaltyType = PenaltyType.ShortNotice;
                penalty.PenaltyPercentage = 10; // 10% penalty for cancellations within 72 hours
                penalty.Description = "Short notice cancellation penalty (within 72 hours of session)";
            }
            else
            {
                penalty.PenaltyType = PenaltyType.None;
                penalty.PenaltyPercentage = 0;
                penalty.Description = "No penalty applicable";
            }

            penalty.PenaltyAmount = booking.Amount * (penalty.PenaltyPercentage / 100.0m);

            // Apply additional penalties for repeat cancellations
            var recentCancellations = await GetRecentCancellationCount(booking.UserId);
            if (recentCancellations >= 3)
            {
                penalty.PenaltyType = PenaltyType.FrequentCancellation;
                penalty.PenaltyPercentage += 10; // Additional 10% for frequent cancellers
                penalty.PenaltyAmount = booking.Amount * (penalty.PenaltyPercentage / 100.0m);
                penalty.Description += " + Frequent cancellation penalty";
            }

            return penalty;
        }

        public async Task<BookingModificationResult> ValidateBookingModificationAsync(int bookingId, string userId, UpdateBookingDto updateDto)
        {
            var result = new BookingModificationResult { IsAllowed = true };

            try
            {
                var booking = await _bookingRepository.GetByIdAsync(bookingId);
                if (booking == null)
                {
                    result.IsAllowed = false;
                    result.Reason = "Booking not found";
                    return result;
                }

                // Check if user can modify this booking
                if (booking.UserId != userId && booking.MentorId != userId)
                {
                    result.IsAllowed = false;
                    result.Reason = "You are not authorized to modify this booking";
                    return result;
                }

                // Check if booking can be modified
                if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
                {
                    result.IsAllowed = false;
                    result.Reason = "Cannot modify cancelled or completed bookings";
                    return result;
                }

                // Check if modification is before the session starts
                if (booking.SessionStartTime <= DateTime.UtcNow.AddHours(2))
                {
                    result.IsAllowed = false;
                    result.Reason = "Cannot modify bookings less than 2 hours before session time";
                    return result;
                }

                // Validate modification constraints
                if (updateDto.Notes != null && updateDto.Notes.Length > 500)
                {
                    result.IsAllowed = false;
                    result.Reason = "Notes cannot exceed 500 characters";
                    return result;
                }

                // Calculate modification fee if applicable
                var timeUntilSession = booking.SessionStartTime - DateTime.UtcNow;
                if (timeUntilSession.TotalHours < 24)
                {
                    result.ModificationFee = booking.Amount * 0.05m; // 5% modification fee within 24 hours
                    result.HasModificationFee = true;
                }

                result.OriginalBooking = booking;
            }
            catch (Exception ex)
            {
                result.IsAllowed = false;
                result.Reason = $"Error validating modification: {ex.Message}";
            }

            return result;
        }

        private decimal CalculateProcessingFee(decimal amount)
        {
            // Fixed processing fee: $5 or 3% of amount, whichever is greater
            var percentageFee = amount * 0.03m;
            return Math.Max(5.0m, percentageFee);
        }

        private bool HasSpecialCircumstances(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                return false;

            var specialCircumstanceKeywords = new[]
            {
                "medical emergency", "family emergency", "death", "illness", "accident",
                "hospital", "doctor", "emergency", "urgent", "unforeseen", "unexpected",
                "force majeure", "act of god", "natural disaster", "pandemic"
            };

            var lowerReason = reason.ToLower();
            return specialCircumstanceKeywords.Any(keyword => lowerReason.Contains(keyword));
        }

        private async Task<int> GetRecentCancellationCount(string userId)
        {
            // Get user's recent cancellations (last 90 days)
            var userBookings = await _bookingRepository.GetByUserIdAsync(userId);
            var ninetyDaysAgo = DateTime.UtcNow.AddDays(-90);

            return userBookings.Count(b =>
                b.Status == BookingStatus.Cancelled &&
                b.CancelledAt.HasValue &&
                b.CancelledAt.Value >= ninetyDaysAgo);
        }
    }

    // Result classes
    public class CancellationPolicyResult
    {
        public bool IsAllowed { get; set; }
        public string? Reason { get; set; }
        public CancellationPolicy? Policy { get; set; }
        public RefundCalculation? RefundCalculation { get; set; }
        public CancellationPenalty? Penalty { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal EstimatedRefundAmount { get; set; }
        public bool HasSpecialCircumstances { get; set; }
    }

    public class RefundCalculation
    {
        public decimal OriginalAmount { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal RefundPercentage { get; set; }
        public string? Currency { get; set; }
        public RefundTier? AppliedTier { get; set; }
    }

    public class CancellationPenalty
    {
        public PenaltyType PenaltyType { get; set; }
        public decimal PenaltyAmount { get; set; }
        public decimal PenaltyPercentage { get; set; }
        public string? Description { get; set; }
    }

    public class BookingModificationResult
    {
        public bool IsAllowed { get; set; }
        public string? Reason { get; set; }
        public Booking? OriginalBooking { get; set; }
        public bool HasModificationFee { get; set; }
        public decimal ModificationFee { get; set; }
    }

    // Policy classes
    public class CancellationPolicy
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public decimal MinimumRefundPercentage { get; set; }
        public decimal ProcessingFeePercentage { get; set; }
        public List<RefundTier> RefundTiers { get; set; } = new List<RefundTier>();
    }

    public class RefundTier
    {
        public double HoursBeforeSession { get; set; }
        public decimal RefundPercentage { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    public enum PenaltyType
    {
        None,
        LateCancellation,
        ShortNotice,
        FrequentCancellation,
        NoShow
    }
}
