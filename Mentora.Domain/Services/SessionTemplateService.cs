using Mentora.Domain.DTOs;
using Mentora.Core.Data;
using Mentora.Domain.Interfaces;
using AutoMapper;

namespace Mentora.Domain.Services;

public class SessionTemplateService : ISessionTemplateService
{
    private readonly ISessionTemplateRepository _templateRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly ISessionService _sessionService;
    private readonly IRecurrenceService _recurrenceService;
    private readonly IMapper _mapper;

    public SessionTemplateService(
        ISessionTemplateRepository templateRepository,
        ISessionRepository sessionRepository,
        ISessionService sessionService,
        IRecurrenceService recurrenceService,
        IMapper mapper)
    {
        _templateRepository = templateRepository;
        _sessionRepository = sessionRepository;
        _sessionService = sessionService;
        _recurrenceService = recurrenceService;
        _mapper = mapper;
    }

    public async Task<ResponseSessionTemplateDto> CreateTemplateAsync(CreateSessionTemplateDto templateDto, string mentorId)
    {
        var template = new SessionTemplate
        {
            MentorId = mentorId,
            Name = templateDto.Name,
            Description = templateDto.Description,
            BasePrice = templateDto.BasePrice,
            Type = templateDto.Type,
            DefaultDuration = templateDto.DefaultDuration,
            DefaultNotes = templateDto.DefaultNotes,
            AllowRecurring = templateDto.AllowRecurring,
            AllowCustomDuration = templateDto.AllowCustomDuration,
            AllowCustomPrice = templateDto.AllowCustomPrice,
            MinimumDurationMinutes = templateDto.MinimumDurationMinutes,
            MaximumDurationMinutes = templateDto.MaximumDurationMinutes
        };

        if (templateDto.DefaultRecurrence != null)
        {
            template.SetDefaultRecurrence(templateDto.DefaultRecurrence);
        }

        var createdTemplate = await _templateRepository.CreateAsync(template);
        return MapToResponseDto(createdTemplate);
    }

    public async Task<ResponseSessionTemplateDto> UpdateTemplateAsync(int id, UpdateSessionTemplateDto templateDto, string mentorId)
    {
        var existingTemplate = await _templateRepository.GetByIdAsync(id);
        if (existingTemplate == null || existingTemplate.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("Template not found or access denied.");
        }

        if (templateDto.Name != null)
            existingTemplate.Name = templateDto.Name;

        if (templateDto.Description != null)
            existingTemplate.Description = templateDto.Description;

        if (templateDto.BasePrice.HasValue)
            existingTemplate.BasePrice = templateDto.BasePrice.Value;

        if (templateDto.Type.HasValue)
            existingTemplate.Type = templateDto.Type.Value;

        if (templateDto.DefaultDuration.HasValue)
            existingTemplate.DefaultDuration = templateDto.DefaultDuration.Value;

        if (templateDto.DefaultNotes != null)
            existingTemplate.DefaultNotes = templateDto.DefaultNotes;

        if (templateDto.AllowRecurring.HasValue)
            existingTemplate.AllowRecurring = templateDto.AllowRecurring.Value;

        if (templateDto.AllowCustomDuration.HasValue)
            existingTemplate.AllowCustomDuration = templateDto.AllowCustomDuration.Value;

        if (templateDto.AllowCustomPrice.HasValue)
            existingTemplate.AllowCustomPrice = templateDto.AllowCustomPrice.Value;

        if (templateDto.MinimumDurationMinutes.HasValue)
            existingTemplate.MinimumDurationMinutes = templateDto.MinimumDurationMinutes.Value;

        if (templateDto.MaximumDurationMinutes.HasValue)
            existingTemplate.MaximumDurationMinutes = templateDto.MaximumDurationMinutes.Value;

        if (templateDto.DefaultRecurrence != null)
            existingTemplate.SetDefaultRecurrence(templateDto.DefaultRecurrence);

        existingTemplate.UpdatedAt = DateTime.UtcNow;

        var updatedTemplate = await _templateRepository.UpdateAsync(existingTemplate);
        return MapToResponseDto(updatedTemplate);
    }

    public async Task<bool> DeleteTemplateAsync(int id, string mentorId)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        if (template == null || template.MentorId != mentorId)
        {
            return false;
        }

        return await _templateRepository.DeleteAsync(id);
    }

    public async Task<ResponseSessionTemplateDto?> GetTemplateByIdAsync(int id)
    {
        var template = await _templateRepository.GetByIdAsync(id);
        return template != null ? MapToResponseDto(template) : null;
    }

    public async Task<IEnumerable<ResponseSessionTemplateDto>> GetTemplatesByMentorAsync(string mentorId)
    {
        var templates = await _templateRepository.GetByMentorIdAsync(mentorId);
        return templates.Select(MapToResponseDto);
    }

    public async Task<IEnumerable<ResponseSessionTemplateDto>> GetActiveTemplatesByMentorAsync(string mentorId)
    {
        var templates = await _templateRepository.GetActiveTemplatesByMentorIdAsync(mentorId);
        return templates.Select(MapToResponseDto);
    }

    public async Task<IEnumerable<ResponseSessionTemplateDto>> GetAllActiveTemplatesAsync()
    {
        var templates = await _templateRepository.GetAllActiveTemplatesAsync();
        return templates.Select(MapToResponseDto);
    }

    public async Task<IEnumerable<ResponseSessionTemplateDto>> SearchTemplatesAsync(string searchTerm, SessionType? type = null)
    {
        var templates = await _templateRepository.SearchTemplatesAsync(searchTerm, type);
        return templates.Select(MapToResponseDto);
    }

    public async Task<ResponseSessionDto> CreateSessionFromTemplateAsync(CreateSessionFromTemplateDto createDto, string mentorId)
    {
        var template = await _templateRepository.GetByIdAsync(createDto.TemplateId);
        if (template == null)
        {
            throw new ArgumentException("Template not found.");
        }

        // Validate template ownership
        if (template.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only use your own templates.");
        }

        var duration = createDto.Duration ?? template.DefaultDuration;
        var price = createDto.Price ?? template.BasePrice;
        var notes = !string.IsNullOrEmpty(createDto.Notes)
            ? createDto.Notes
            : template.DefaultNotes;

        // Validate duration constraints
        if (!template.AllowCustomDuration)
        {
            duration = template.DefaultDuration;
        }
        else
        {
            var durationMinutes = (int)duration.TotalMinutes;
            if (durationMinutes < template.MinimumDurationMinutes || durationMinutes > template.MaximumDurationMinutes)
            {
                throw new ArgumentException($"Duration must be between {template.MinimumDurationMinutes} and {template.MaximumDurationMinutes} minutes.");
            }
        }

        // Validate price constraints
        if (!template.AllowCustomPrice)
        {
            price = template.BasePrice;
        }

        var sessionDto = new CreateSessionDto
        {
            StartAt = createDto.StartAt,
            Type = template.Type,
            Price = price,
            Notes = notes,
            IsRecurring = createDto.IsRecurring ?? false,
            Recurrence = createDto.IsRecurring == true ? createDto.Recurrence : template.GetDefaultRecurrence()
        };

        // Validate recurring constraints
        if (sessionDto.IsRecurring && !template.AllowRecurring)
        {
            throw new ArgumentException("This template does not allow recurring sessions.");
        }

        var result = await _sessionService.CreateSessionAsync(sessionDto, mentorId);

        // Update template usage statistics
        await _templateRepository.IncrementUsageCountAsync(createDto.TemplateId);

        return result;
    }

    public async Task<List<ResponseSessionDto>> CreateRecurringSessionFromTemplateAsync(CreateSessionFromTemplateDto createDto, string mentorId)
    {
        var template = await _templateRepository.GetByIdAsync(createDto.TemplateId);
        if (template == null)
        {
            throw new ArgumentException("Template not found.");
        }

        if (template.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("You can only use your own templates.");
        }

        if (!template.AllowRecurring)
        {
            throw new ArgumentException("This template does not allow recurring sessions.");
        }

        var duration = createDto.Duration ?? template.DefaultDuration;
        var price = createDto.Price ?? template.BasePrice;
        var notes = !string.IsNullOrEmpty(createDto.Notes)
            ? createDto.Notes
            : template.DefaultNotes;

        var recurringDto = new CreateRecurringSessionDto
        {
            StartDate = createDto.StartAt.Date,
            StartTime = createDto.StartAt.TimeOfDay,
            Duration = duration,
            Price = price,
            Type = template.Type,
            Notes = notes,
            Recurrence = createDto.Recurrence ?? template.GetDefaultRecurrence() ?? new RecurrenceDetails()
        };

        var result = await _sessionService.CreateRecurringSessionAsync(recurringDto, mentorId);

        // Update template usage statistics
        await _templateRepository.IncrementUsageCountAsync(createDto.TemplateId);

        return result;
    }

    public async Task<TemplateUsageStatsDto> GetTemplateUsageStatsAsync(int templateId, string mentorId)
    {
        var template = await _templateRepository.GetByIdAsync(templateId);
        if (template == null || template.MentorId != mentorId)
        {
            throw new UnauthorizedAccessException("Template not found or access denied.");
        }

        var monthlyUsage = new List<MonthlyUsageDto>();
        var sessions = await _sessionRepository.GetByMentorIdAsync(mentorId);
        var templateSessions = sessions.Where(s => s.StartAt >= template.CreatedAt);

        var groupedByMonth = templateSessions
            .Where(s => s.StartAt >= template.CreatedAt)
            .GroupBy(s => new { s.StartAt.Year, s.StartAt.Month })
            .Select(g => new MonthlyUsageDto
            {
                Month = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                Count = g.Count()
            })
            .OrderBy(m => m.Month)
            .ToList();

        return new TemplateUsageStatsDto
        {
            TemplateId = template.Id,
            TemplateName = template.Name,
            TotalUsage = template.UsageCount,
            LastUsedAt = template.LastUsedAt,
            MonthlyUsage = groupedByMonth
        };
    }

    public async Task<IEnumerable<TemplateUsageStatsDto>> GetAllTemplateUsageStatsAsync(string mentorId)
    {
        var templates = await _templateRepository.GetActiveTemplatesByMentorIdAsync(mentorId);
        var statsTasks = templates.Select(t => GetTemplateUsageStatsAsync(t.Id, mentorId));
        return await Task.WhenAll(statsTasks);
    }

    public async Task<IEnumerable<ResponseSessionTemplateDto>> GetPopularTemplatesAsync(int limit = 10)
    {
        var templates = await _templateRepository.GetPopularTemplatesAsync(limit);
        return templates.Select(MapToResponseDto);
    }

    private ResponseSessionTemplateDto MapToResponseDto(SessionTemplate template)
    {
        return new ResponseSessionTemplateDto
        {
            Id = template.Id,
            MentorId = template.MentorId,
            Name = template.Name,
            Description = template.Description,
            BasePrice = template.BasePrice,
            Type = template.Type,
            DefaultDuration = template.DefaultDuration,
            DefaultNotes = template.DefaultNotes,
            AllowRecurring = template.AllowRecurring,
            AllowCustomDuration = template.AllowCustomDuration,
            AllowCustomPrice = template.AllowCustomPrice,
            MinimumDurationMinutes = template.MinimumDurationMinutes,
            MaximumDurationMinutes = template.MaximumDurationMinutes,
            DefaultRecurrence = template.GetDefaultRecurrence(),
            IsActive = template.IsActive,
            CreatedAt = template.CreatedAt,
            UpdatedAt = template.UpdatedAt,
            UsageCount = template.UsageCount,
            LastUsedAt = template.LastUsedAt
        };
    }
}
