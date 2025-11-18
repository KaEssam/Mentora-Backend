using Mentora.APIs.DTOs;
using Mentora.Domain.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Mentora.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _service;
        public SessionController(ISessionService service)
        {
            _service = service;
        }


        [HttpPost]
        public async Task<IActionResult> CreateSessionAsync(CreateSessionDto sessionDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var res = await _service.CreateSessionAsync(sessionDto, mentorId);

            return Ok(res);
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
    }
}
