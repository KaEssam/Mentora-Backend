using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mentora.Domain.DTOs;
using Mentora.Domain.Interfaces;
using Mentora.Core.Data;
using System.Security.Claims;

namespace Mentora.APIs.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class SessionTemplateController : ControllerBase
    {
        private readonly ISessionTemplateService _templateService;

        public SessionTemplateController(ISessionTemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetMyTemplates()
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var templates = await _templateService.GetActiveTemplatesByMentorAsync(mentorId);
            return Ok(templates);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllTemplates()
        {
            var templates = await _templateService.GetAllActiveTemplatesAsync();
            return Ok(templates);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplateById(int id)
        {
            var template = await _templateService.GetTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound();
            }

            return Ok(template);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchTemplates([FromQuery] string searchTerm, [FromQuery] SessionType? type)
        {
            var templates = await _templateService.SearchTemplatesAsync(searchTerm, type);
            return Ok(templates);
        }

        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularTemplates([FromQuery] int limit = 10)
        {
            var templates = await _templateService.GetPopularTemplatesAsync(limit);
            return Ok(templates);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] CreateSessionTemplateDto templateDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _templateService.CreateTemplateAsync(templateDto, mentorId);
            return CreatedAtAction(nameof(GetTemplateById), new { id = result.Id }, result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(int id, [FromBody] UpdateSessionTemplateDto templateDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var result = await _templateService.UpdateTemplateAsync(id, templateDto, mentorId);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var success = await _templateService.DeleteTemplateAsync(id, mentorId);
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost("{id}/session")]
        public async Task<IActionResult> CreateSessionFromTemplate(int id, [FromBody] CreateSessionFromTemplateDto createDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            createDto.TemplateId = id; // Ensure template ID is set from URL

            var result = await _templateService.CreateSessionFromTemplateAsync(createDto, mentorId);
            return CreatedAtAction(nameof(GetMyTemplates), null, result);
        }

        [HttpPost("{id}/recurring-session")]
        public async Task<IActionResult> CreateRecurringSessionFromTemplate(int id, [FromBody] CreateSessionFromTemplateDto createDto)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            createDto.TemplateId = id; // Ensure template ID is set from URL
            createDto.IsRecurring = true; // Force recurring for this endpoint

            var result = await _templateService.CreateRecurringSessionFromTemplateAsync(createDto, mentorId);
            return CreatedAtAction(nameof(GetMyTemplates), null, result);
        }

        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetTemplateStats(int id)
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var stats = await _templateService.GetTemplateUsageStatsAsync(id, mentorId);
            return Ok(stats);
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetAllTemplateStats()
        {
            var mentorId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var stats = await _templateService.GetAllTemplateUsageStatsAsync(mentorId);
            return Ok(stats);
        }
    }
}
