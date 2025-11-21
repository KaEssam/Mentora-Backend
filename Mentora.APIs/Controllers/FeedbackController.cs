using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using System.Security.Claims;

namespace Mentora.APIs.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class FeedbackController : ControllerBase
{
    private readonly ISessionFeedbackService _feedbackService;
    private readonly ILogger<FeedbackController> _logger;

    public FeedbackController(
        ISessionFeedbackService feedbackService,
        ILogger<FeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetUserFeedback()
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var feedbacks = await _feedbackService.GetUserFeedbackAsync(UserId);
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseFeedbackDto>> GetFeedback(int id)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var feedback = await _feedbackService.GetFeedbackByIdAsync(id, UserId);
            if (feedback == null)
                return NotFound();

            return Ok(feedback);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback {FeedbackId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetSessionFeedback(int sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var feedbacks = await _feedbackService.GetFeedbackBySessionIdAsync(sessionId, UserId);
            return Ok(feedbacks);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback for session {SessionId} and user {UserId}", sessionId, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("mentor/{mentorId}")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetMentorFeedback(string mentorId)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var feedbacks = await _feedbackService.GetMentorFeedbackAsync(mentorId, UserId);
            return Ok(feedbacks);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mentor feedback for {MentorId} and user {UserId}", mentorId, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("mentor/{mentorId}/public")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetPublicMentorFeedback(string mentorId)
    {
        try
        {
            var feedbacks = await _feedbackService.GetPublicMentorFeedbackAsync(mentorId);
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving public mentor feedback for {MentorId}", mentorId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ResponseFeedbackDto>> CreateFeedback([FromBody] CreateFeedbackDto createDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var feedback = await _feedbackService.CreateFeedbackAsync(createDto, UserId);
            return CreatedAtAction(nameof(GetFeedback), new { id = feedback.Id }, feedback);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating feedback for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseFeedbackDto>> UpdateFeedback(int id, [FromBody] UpdateFeedbackDto updateDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var feedback = await _feedbackService.UpdateFeedbackAsync(id, updateDto, UserId);
            if (feedback == null)
                return NotFound();

            return Ok(feedback);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating feedback {FeedbackId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFeedback(int id)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var success = await _feedbackService.DeleteFeedbackAsync(id, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting feedback {FeedbackId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToFeedback(int id, [FromBody] MentorFeedbackResponseDto responseDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _feedbackService.RespondToFeedbackAsync(id, responseDto, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error responding to feedback {FeedbackId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/flag")]
    public async Task<IActionResult> FlagFeedback(int id, [FromBody] FeedbackFlagDto flagDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _feedbackService.FlagFeedbackAsync(id, flagDto, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging feedback {FeedbackId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("rating-range")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetFeedbackByRatingRange(
        [FromQuery] int minRating = 1,
        [FromQuery] int maxRating = 5)
    {
        try
        {
            if (minRating < 1 || minRating > 5 || maxRating < 1 || maxRating > 5 || minRating > maxRating)
            {
                return BadRequest("Invalid rating range. Ratings must be between 1 and 5.");
            }

            var feedbacks = await _feedbackService.GetFeedbackByRatingRangeAsync(minRating, maxRating);
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback by rating range");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<FeedbackStatsDto>> GetFeedbackStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var stats = await _feedbackService.GetFeedbackStatsAsync(UserId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving feedback stats for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    // Admin-only endpoints
    [HttpGet("moderation/flagged")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetFlaggedFeedback()
    {
        try
        {
            var feedbacks = await _feedbackService.GetFlaggedFeedbackAsync();
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving flagged feedback");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("moderation/hidden")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<IEnumerable<ResponseFeedbackDto>>> GetHiddenFeedback()
    {
        try
        {
            var feedbacks = await _feedbackService.GetHiddenFeedbackAsync();
            return Ok(feedbacks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving hidden feedback");
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/moderate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ModerateFeedback(int id, [FromBody] FeedbackModerationDto moderationDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _feedbackService.ModerateFeedbackAsync(id, moderationDto);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moderating feedback {FeedbackId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("moderation/bulk")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> BulkModerateFeedback([FromBody] BulkFeedbackActionDto bulkActionDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var success = await _feedbackService.BulkModerateFeedbackAsync(bulkActionDto);
            return Ok(new { success });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk moderating feedback");
            return StatusCode(500, "Internal server error");
        }
    }
}
