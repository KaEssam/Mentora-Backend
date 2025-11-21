using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using System.Security.Claims;

namespace Mentora.APIs.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReminderController : ControllerBase
{
    private readonly IReminderService _reminderService;
    private readonly ILogger<ReminderController> _logger;

    public ReminderController(
        IReminderService reminderService,
        ILogger<ReminderController> logger)
    {
        _reminderService = reminderService;
        _logger = logger;
    }

    private string? UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResponseReminderDto>>> GetUserReminders()
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var reminders = await _reminderService.GetUserRemindersAsync(UserId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminders for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ResponseReminderDto>> GetReminder(int id)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var reminder = await _reminderService.GetReminderByIdAsync(id, UserId);
            if (reminder == null)
                return NotFound();

            return Ok(reminder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminder {ReminderId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("session/{sessionId}")]
    public async Task<ActionResult<IEnumerable<ResponseReminderDto>>> GetSessionReminders(int sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var reminders = await _reminderService.GetSessionRemindersAsync(sessionId, UserId);
            return Ok(reminders);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminders for session {SessionId} and user {UserId}", sessionId, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ResponseReminderDto>> CreateReminder([FromBody] CreateReminderDto createDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reminder = await _reminderService.CreateReminderAsync(createDto, UserId);
            return CreatedAtAction(nameof(GetReminder), new { id = reminder.Id }, reminder);
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
            _logger.LogError(ex, "Error creating reminder for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("schedule/{sessionId}")]
    public async Task<ActionResult<IEnumerable<ResponseReminderDto>>> ScheduleSessionReminders(int sessionId)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var reminders = await _reminderService.ScheduleSessionRemindersAsync(sessionId);
            return Ok(reminders);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling reminders for session {SessionId} and user {UserId}", sessionId, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("bulk-schedule")]
    public async Task<ActionResult<IEnumerable<ResponseReminderDto>>> ScheduleBulkReminders([FromBody] BulkScheduleRemindersDto bulkDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reminders = await _reminderService.ScheduleBulkRemindersAsync(bulkDto, UserId);
            return Ok(reminders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk scheduling reminders for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ResponseReminderDto>> UpdateReminder(int id, [FromBody] UpdateReminderDto updateDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reminder = await _reminderService.UpdateReminderAsync(id, updateDto, UserId);
            if (reminder == null)
                return NotFound();

            return Ok(reminder);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reminder {ReminderId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("{id}/reschedule")]
    public async Task<IActionResult> RescheduleReminder(int id, [FromBody] DateTime newScheduledAt)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var success = await _reminderService.RescheduleReminderAsync(id, newScheduledAt, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rescheduling reminder {ReminderId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReminder(int id)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var success = await _reminderService.DeleteReminderAsync(id, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting reminder {ReminderId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelReminder(int id)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var success = await _reminderService.CancelReminderAsync(id, UserId);
            if (!success)
                return NotFound();

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling reminder {ReminderId} for user {UserId}", id, UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPost("{id}/send")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SendReminderManually(int id)
    {
        try
        {
            var success = await _reminderService.SendReminderAsync(id);
            if (!success)
                return BadRequest("Failed to send reminder");

            return Ok("Reminder sent successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending reminder {ReminderId} manually", id);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpGet("stats")]
    public async Task<ActionResult<ReminderStatsDto>> GetReminderStats(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var stats = await _reminderService.GetReminderStatsAsync(UserId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminder stats for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    // Reminder Settings endpoints
    [HttpGet("settings")]
    public async Task<ActionResult<ReminderSettingsDto>> GetReminderSettings()
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            var settings = await _reminderService.GetReminderSettingsAsync(UserId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving reminder settings for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }

    [HttpPut("settings")]
    public async Task<ActionResult<ReminderSettingsDto>> UpdateReminderSettings([FromBody] UpdateReminderSettingsDto updateDto)
    {
        try
        {
            if (string.IsNullOrEmpty(UserId))
                return Unauthorized();

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var settings = await _reminderService.UpdateReminderSettingsAsync(updateDto, UserId);
            return Ok(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating reminder settings for user {UserId}", UserId);
            return StatusCode(500, "Internal server error");
        }
    }
}
