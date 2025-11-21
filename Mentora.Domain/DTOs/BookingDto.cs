using System;
using System.ComponentModel.DataAnnotations;
using Mentora.Core.Data;
using Mentora.Domain.Services;

namespace Mentora.Domain.DTOs
{
    public class BookingDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string MentorId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime BookingDate { get; set; }
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public BookingStatus Status { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? MeetingUrl { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CancelledAt { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentIntentId { get; set; }
    }

    public class CreateBookingDto
    {
        [Required]
        public string SessionId { get; set; } = string.Empty;

        [Required]
        public DateTime SessionStartTime { get; set; }

        [Required]
        public DateTime SessionEndTime { get; set; }

        [Required]
        [Range(0.01, 10000.00)]
        public decimal Amount { get; set; }

        [Required]
        public string Currency { get; set; } = "USD";

        [StringLength(500)]
        public string? Notes { get; set; }
    }

    public class UpdateBookingDto
    {
        [StringLength(500)]
        public string? Notes { get; set; }

        public string? MeetingUrl { get; set; }

        public bool? IsPaid { get; set; }

        public string? PaymentIntentId { get; set; }
    }

    public class CancelBookingDto
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class BookingConfirmationDto
    {
        public int BookingId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string MentorId { get; set; } = string.Empty;
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string MeetingUrl { get; set; } = string.Empty;
    }

    public class BookingListDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string MentorId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public BookingStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsUpcoming => SessionStartTime > DateTime.UtcNow;
        public bool IsPast => SessionEndTime < DateTime.UtcNow;
    }

    public class BookingStatsDto
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ConfirmedBookings { get; set; }
        public int CompletedBookings { get; set; }
        public int CancelledBookings { get; set; }
        public decimal TotalRevenue { get; set; }
        public decimal MonthlyRevenue { get; set; }
    }

    public class CancellationEvaluationRequest
    {
        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;
    }

    public class BookingCancellationResult
    {
        public BookingDto Booking { get; set; } = new BookingDto();
        public decimal RefundAmount { get; set; }
        public decimal ProcessingFee { get; set; }
        public decimal PenaltyAmount { get; set; }
        public bool HasSpecialCircumstances { get; set; }
        public CancellationPolicy? AppliedPolicy { get; set; }
        public RefundCalculation? RefundCalculation { get; set; }
        public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    }
}
