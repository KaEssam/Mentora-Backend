using System.Text.Json;
using Mentora.Core.Data;
using Mentora.Domain.Services;

namespace Mentora.Infra.Services;

public class RecurrenceService : IRecurrenceService
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public List<DateTime> GenerateRecurringDates(DateTime startDate, RecurrenceDetails recurrence)
    {
        var dates = new List<DateTime>();
        if (recurrence.Pattern == RecurrencePattern.None)
        {
            dates.Add(startDate);
            return dates;
        }

        var currentDate = startDate;
        int occurrenceCount = 0;
        int interval = recurrence.Interval ?? 1;

        while (true)
        {
            // Check end conditions
            if (recurrence.EndDate.HasValue && currentDate > recurrence.EndDate.Value)
                break;

            if (recurrence.MaxOccurrences.HasValue && occurrenceCount >= recurrence.MaxOccurrences.Value)
                break;

            // Check if this date should be included
            if (ShouldIncludeDate(currentDate, recurrence))
            {
                dates.Add(currentDate);
                occurrenceCount++;
            }

            // Move to next date
            currentDate = GetNextDate(currentDate, recurrence.Pattern, interval);

            // Prevent infinite loop
            if (occurrenceCount > 1000) break;
        }

        return dates;
    }

    public string SerializeRecurrence(RecurrenceDetails recurrence)
    {
        return JsonSerializer.Serialize(recurrence, _jsonOptions);
    }

    public RecurrenceDetails DeserializeRecurrence(string recurrenceJson)
    {
        if (string.IsNullOrEmpty(recurrenceJson))
            return new RecurrenceDetails();

        return JsonSerializer.Deserialize<RecurrenceDetails>(recurrenceJson, _jsonOptions)
               ?? new RecurrenceDetails();
    }

    public DateTime GetNextOccurrence(DateTime currentDate, RecurrenceDetails recurrence)
    {
        if (recurrence.Pattern == RecurrencePattern.None)
            return currentDate;

        int interval = recurrence.Interval ?? 1;
        return GetNextDate(currentDate, recurrence.Pattern, interval);
    }

    public bool IsDateInRecurrence(DateTime date, RecurrenceDetails recurrence)
    {
        if (recurrence.Pattern == RecurrencePattern.None)
            return false;

        // Check if date is excluded
        if (recurrence.ExcludedDates.Any(excludedDate => excludedDate.Date == date.Date))
            return false;

        return ShouldIncludeDate(date, recurrence);
    }

    private bool ShouldIncludeDate(DateTime date, RecurrenceDetails recurrence)
    {
        return recurrence.Pattern switch
        {
            RecurrencePattern.Daily => true,
            RecurrencePattern.Weekly => recurrence.DaysOfWeek.Count == 0 ||
                                      recurrence.DaysOfWeek.Contains(date.DayOfWeek),
            RecurrencePattern.BiWeekly => recurrence.DaysOfWeek.Count == 0 ||
                                        recurrence.DaysOfWeek.Contains(date.DayOfWeek),
            RecurrencePattern.Monthly => !recurrence.DayOfMonth.HasValue ||
                                       date.Day == recurrence.DayOfMonth.Value,
            RecurrencePattern.Custom => true, // Custom logic would be implemented here
            _ => false
        };
    }

    private DateTime GetNextDate(DateTime currentDate, RecurrencePattern pattern, int interval)
    {
        return pattern switch
        {
            RecurrencePattern.Daily => currentDate.AddDays(interval),
            RecurrencePattern.Weekly => currentDate.AddDays(7 * interval),
            RecurrencePattern.BiWeekly => currentDate.AddDays(14 * interval),
            RecurrencePattern.Monthly => currentDate.AddMonths(interval),
            RecurrencePattern.Custom => currentDate.AddDays(interval), // Default for custom
            _ => currentDate
        };
    }
}
