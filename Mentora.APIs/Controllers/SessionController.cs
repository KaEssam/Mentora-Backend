using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mentora.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _service;
        public SessionController(ISessionService service)
        {
            _service = service;
        }

        [HttpPost]
        public async Task<IActionResult> CreateSessionAsync([FromBody] CreateSessionDto sessionDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.CreateSessionAsync(sessionDto, mentorId);
            return Ok(result);
        }

        [HttpPost("recurring")]
        public async Task<IActionResult> CreateRecurringSessionAsync([FromBody] CreateRecurringSessionDto sessionDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _service.CreateRecurringSessionAsync(sessionDto, mentorId);
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSessionByIdAsync(int id)
        {
            var session = await _service.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            return Ok(session);
        }

        [HttpGet("{id}/instances")]
        public async Task<IActionResult> GetRecurringSessionInstancesAsync(int id)
        {
            var instances = await _service.GetRecurringSessionInstancesAsync(id);
            return Ok(instances);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSessionAsync(int id, [FromBody] CreateSessionDto sessionDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var session = await _service.GetSessionByIdAsync(id);
            if (session == null)
            {
                return NotFound();
            }

            // Update session properties
            session.StartAt = sessionDto.StartAt;
            session.EndAt = sessionDto.StartAt.AddMinutes(30);
            session.Price = sessionDto.Price;
            session.Notes = sessionDto.Notes;
            if (sessionDto.Type.HasValue)
            {
                session.Type = sessionDto.Type.Value;
            }

            var updatedSession = await _service.UpdateSessionAsync(session, mentorId);
            return Ok(updatedSession);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSessionAsync(int id)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var result = await _service.DeleteSessionAsync(id, mentorId);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAvailableSessionsAsync()
        {
            var sessions = await _service.GetAvailableSessionsAsync();
            return Ok(sessions);
        }

        [HttpGet("mentor/{mentorId}")]
        public async Task<IActionResult> GetSessionsByMentorAsync(string mentorId)
        {
            var sessions = await _service.GetSessionsByMentorAsync(mentorId);
            return Ok(sessions);
        }

        [HttpGet("range")]
        public async Task<IActionResult> GetSessionsByDateRangeAsync([FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            var sessions = await _service.GetSessionsByDateRangeAsync(startDate, endDate);
            return Ok(sessions);
        }
    }
}
