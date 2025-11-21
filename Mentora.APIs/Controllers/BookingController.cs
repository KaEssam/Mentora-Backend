using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Mentora.Core.Data;
using Mentora.Domain.DTOs;
using Mentora.Domain.Services;
using Mentora.Domain.Interfaces;

namespace Mentora.APIs.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        private readonly IBookingValidationService _bookingValidationService;
        private readonly ICancellationPolicyService _cancellationPolicyService;

        public BookingController(
            IBookingService bookingService,
            IBookingValidationService bookingValidationService,
            ICancellationPolicyService cancellationPolicyService)
        {
            _bookingService = bookingService;
            _bookingValidationService = bookingValidationService;
            _cancellationPolicyService = cancellationPolicyService;
        }

        /// <summary>
        /// Validate booking request before creating
        /// </summary>
        /// <param name="bookingDto">Booking creation data to validate</param>
        /// <returns>Validation result with any errors</returns>
        [HttpPost("validate")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingValidationResult>> ValidateBooking([FromBody] CreateBookingDto bookingDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var validationResult = await _bookingValidationService.ValidateBookingRequestAsync(userId, bookingDto);
                return Ok(validationResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Validation error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check time slot availability for a mentor
        /// </summary>
        /// <param name="mentorId">Mentor ID</param>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="excludeBookingId">Optional booking ID to exclude from conflict check</param>
        /// <returns>Validation result showing if time slot is available</returns>
        [HttpGet("validate-time-slot")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BookingValidationResult>> ValidateTimeSlot(
            [FromQuery] string mentorId,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] int? excludeBookingId = null)
        {
            if (string.IsNullOrEmpty(mentorId))
            {
                return BadRequest("Mentor ID is required");
            }

            var validationResult = await _bookingValidationService.ValidateTimeSlotAsync(mentorId, startTime, endTime, excludeBookingId);
            return Ok(validationResult);
        }

        /// <summary>
        /// Check for booking conflicts
        /// </summary>
        /// <param name="mentorId">Mentor ID</param>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <param name="excludeBookingId">Optional booking ID to exclude from conflict check</param>
        /// <returns>Conflict detection result</returns>
        [HttpGet("check-conflicts")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<BookingConflictResult>> CheckConflicts(
            [FromQuery] string mentorId,
            [FromQuery] DateTime startTime,
            [FromQuery] DateTime endTime,
            [FromQuery] int? excludeBookingId = null)
        {
            if (string.IsNullOrEmpty(mentorId))
            {
                return BadRequest("Mentor ID is required");
            }

            var conflictResult = await _bookingValidationService.CheckBookingConflictsAsync(mentorId, startTime, endTime, excludeBookingId);
            return Ok(conflictResult);
        }

        /// <summary>
        /// Get mentor availability for a date range
        /// </summary>
        /// <param name="mentorId">Mentor ID</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns>Mentor availability information</returns>
        [HttpGet("mentor-availability/{mentorId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<MentorAvailabilityResult>> GetMentorAvailability(
            string mentorId,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            if (string.IsNullOrEmpty(mentorId))
            {
                return BadRequest("Mentor ID is required");
            }

            if (startDate >= endDate)
            {
                return BadRequest("Start date must be before end date");
            }

            if ((endDate - startDate).TotalDays > 31)
            {
                return BadRequest("Date range cannot exceed 31 days");
            }

            var availabilityResult = await _bookingValidationService.GetMentorAvailabilityAsync(mentorId, startDate, endDate);
            return Ok(availabilityResult);
        }

        /// <summary>
        /// Get available time slot suggestions for a mentor
        /// </summary>
        /// <param name="mentorId">Mentor ID</param>
        /// <param name="preferredDate">Preferred date for booking</param>
        /// <param name="durationMinutes">Duration of booking in minutes</param>
        /// <param name="suggestionCount">Number of suggestions to return</param>
        /// <returns>List of available time slot suggestions</returns>
        [HttpGet("suggest-time-slots")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<List<TimeSlotSuggestion>>> SuggestTimeSlots(
            [FromQuery] string mentorId,
            [FromQuery] DateTime preferredDate,
            [FromQuery] int durationMinutes,
            [FromQuery] int suggestionCount = 5)
        {
            if (string.IsNullOrEmpty(mentorId))
            {
                return BadRequest("Mentor ID is required");
            }

            if (durationMinutes < 15 || durationMinutes > 480) // 15 min to 8 hours
            {
                return BadRequest("Duration must be between 15 and 480 minutes");
            }

            if (suggestionCount < 1 || suggestionCount > 20)
            {
                return BadRequest("Suggestion count must be between 1 and 20");
            }

            var suggestions = await _bookingValidationService.SuggestAvailableTimeSlotsAsync(
                mentorId, preferredDate, durationMinutes, suggestionCount);
            return Ok(suggestions);
        }

        /// <summary>
        /// Create a new booking
        /// </summary>
        /// <param name="bookingDto">Booking creation data</param>
        /// <returns>Created booking details</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingDto>> CreateBooking([FromBody] CreateBookingDto bookingDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                // Pre-validate booking request
                var validationResult = await _bookingValidationService.ValidateBookingRequestAsync(userId, bookingDto);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new
                    {
                        Message = "Booking validation failed",
                        Errors = validationResult.Errors
                    });
                }

                var booking = await _bookingService.CreateBookingAsync(userId, bookingDto);
                return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get booking by ID
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingDto>> GetBooking(int id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var booking = await _bookingService.GetBookingByIdAsync(id, userId);
                if (booking == null)
                {
                    return NotFound();
                }

                return Ok(booking);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get current user's bookings
        /// </summary>
        /// <returns>List of user bookings</returns>
        [HttpGet("my-bookings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<BookingListDto>>> GetMyBookings()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var bookings = await _bookingService.GetUserBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get mentor's bookings (for mentors)
        /// </summary>
        /// <returns>List of mentor bookings</returns>
        [HttpGet("mentor-bookings")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<BookingListDto>>> GetMentorBookings()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                // Check if user is a mentor (this would be enhanced with proper role checking)
                // For now, we'll assume all authenticated users can access their bookings
                var bookings = await _bookingService.GetMentorBookingsAsync(userId);
                return Ok(bookings);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Update booking details
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="bookingDto">Booking update data</param>
        /// <returns>Updated booking details</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingDto>> UpdateBooking(int id, [FromBody] UpdateBookingDto bookingDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var booking = await _bookingService.UpdateBookingAsync(id, userId, bookingDto);
                return Ok(booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get available cancellation policies
        /// </summary>
        /// <returns>List of available cancellation policies</returns>
        [HttpGet("cancellation-policies")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<CancellationPolicy>>> GetCancellationPolicies()
        {
            var policies = await _cancellationPolicyService.GetAvailablePoliciesAsync();
            return Ok(policies);
        }

        /// <summary>
        /// Evaluate cancellation policy for a specific booking
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="evaluationRequest">Cancellation evaluation request</param>
        /// <returns>Cancellation policy evaluation result</returns>
        [HttpPost("{id}/evaluate-cancellation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<CancellationPolicyResult>> EvaluateCancellationPolicy(
            int id, [FromBody] CancellationEvaluationRequest evaluationRequest)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var policyResult = await _cancellationPolicyService.EvaluateCancellationPolicyAsync(
                    id, userId, evaluationRequest.Reason);

                return Ok(policyResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error evaluating cancellation policy: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a booking can be cancelled
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking cancellation eligibility</returns>
        [HttpGet("{id}/can-cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<object>> CanCancelBooking(int id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var booking = await _bookingService.GetBookingByIdAsync(id, userId);
                if (booking == null)
                {
                    return NotFound();
                }

                // Convert BookingDto back to Booking entity for validation
                // In a real implementation, we'd extend the repository or service to return the full entity
                var canCancel = false;
                var reason = "";

                try
                {
                    // Basic checks - in a real implementation, this would use the actual booking entity
                    canCancel = booking.Status != BookingStatus.Cancelled &&
                               booking.Status != BookingStatus.Completed &&
                               booking.SessionStartTime > DateTime.UtcNow.AddHours(2);

                    if (!canCancel)
                    {
                        reason = booking.Status == BookingStatus.Cancelled ? "Already cancelled" :
                                booking.Status == BookingStatus.Completed ? "Already completed" :
                                "Less than 2 hours before session time";
                    }
                }
                catch
                {
                    canCancel = false;
                    reason = "Unable to determine cancellation eligibility";
                }

                return Ok(new { CanCancel = canCancel, Reason = reason });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error checking cancellation eligibility: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancel a booking with policy evaluation
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="cancelDto">Cancellation request with reason</param>
        /// <returns>Updated booking details with refund information</returns>
        [HttpPost("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingCancellationResult>> CancelBooking(int id, [FromBody] CancelBookingDto cancelDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                // Evaluate cancellation policy first
                var policyResult = await _cancellationPolicyService.EvaluateCancellationPolicyAsync(id, userId, cancelDto.Reason);

                if (!policyResult.IsAllowed)
                {
                    return BadRequest(new
                    {
                        Message = "Cancellation not allowed",
                        Reason = policyResult.Reason
                    });
                }

                // Proceed with cancellation
                var booking = await _bookingService.CancelBookingAsync(id, userId, cancelDto);

                var cancellationResult = new BookingCancellationResult
                {
                    Booking = booking,
                    RefundAmount = policyResult.EstimatedRefundAmount,
                    ProcessingFee = policyResult.ProcessingFee,
                    PenaltyAmount = policyResult.Penalty?.PenaltyAmount ?? 0,
                    HasSpecialCircumstances = policyResult.HasSpecialCircumstances,
                    AppliedPolicy = policyResult.Policy,
                    RefundCalculation = policyResult.RefundCalculation
                };

                return Ok(cancellationResult);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error cancelling booking: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate booking modification before applying changes
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <param name="updateDto">Booking update data</param>
        /// <returns>Booking modification validation result</returns>
        [HttpPost("{id}/validate-modification")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingModificationResult>> ValidateBookingModification(
            int id, [FromBody] UpdateBookingDto updateDto)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var modificationResult = await _cancellationPolicyService.ValidateBookingModificationAsync(id, userId, updateDto);
                return Ok(modificationResult);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error validating booking modification: {ex.Message}");
            }
        }

        /// <summary>
        /// Confirm a booking (for mentors)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Updated booking details</returns>
        [HttpPost("{id}/confirm")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BookingDto>> ConfirmBooking(int id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var booking = await _bookingService.ConfirmBookingAsync(id, userId);
                return Ok(booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Complete a booking (for mentors)
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Updated booking details</returns>
        [HttpPost("{id}/complete")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BookingDto>> CompleteBooking(int id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var booking = await _bookingService.CompleteBookingAsync(id, userId);
                return Ok(booking);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Check if a time slot is available for booking
        /// </summary>
        /// <param name="mentorId">Mentor ID</param>
        /// <param name="startTime">Start time</param>
        /// <param name="endTime">End time</param>
        /// <returns>Availability status</returns>
        [HttpGet("check-availability")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<bool>> CheckAvailability([FromQuery] string mentorId, [FromQuery] DateTime startTime, [FromQuery] DateTime endTime)
        {
            if (string.IsNullOrEmpty(mentorId))
            {
                return BadRequest("Mentor ID is required");
            }

            if (endTime <= startTime)
            {
                return BadRequest("End time must be after start time");
            }

            var isAvailable = await _bookingService.IsTimeSlotAvailableAsync(mentorId, startTime, endTime);
            return Ok(isAvailable);
        }

        /// <summary>
        /// Get booking confirmation details
        /// </summary>
        /// <param name="id">Booking ID</param>
        /// <returns>Booking confirmation details</returns>
        [HttpGet("{id}/confirmation")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingConfirmationDto>> GetBookingConfirmation(int id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var confirmation = await _bookingService.GetBookingConfirmationAsync(id, userId);
                return Ok(confirmation);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get current user's booking statistics
        /// </summary>
        /// <returns>User booking statistics</returns>
        [HttpGet("my-stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<BookingStatsDto>> GetMyBookingStats()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var stats = await _bookingService.GetUserBookingStatsAsync(userId);
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }

        /// <summary>
        /// Get mentor's booking statistics (for mentors)
        /// </summary>
        /// <returns>Mentor booking statistics</returns>
        [HttpGet("mentor-stats")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<BookingStatsDto>> GetMentorBookingStats()
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value ?? User.FindFirst("nameid")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized("User ID not found in token");
                }

                var stats = await _bookingService.GetMentorBookingStatsAsync(userId);
                return Ok(stats);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
        }
    }
}
