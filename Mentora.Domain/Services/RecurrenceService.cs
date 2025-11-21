using System.Text.Json;
using Mentora.Core.Data;

namespace Mentora.Domain.Services;

public class RecurrenceService : IRecurrenceService
{
    public List<DateTime> GenerateRecurringDates(DateTime startDate, RecurrenceDetails recurrence)
    {
        var dates = new List<DateTime>();

        if (recurrence.Pattern == RecurrencePattern.None)
        {
            dates.Add(startDate);
            return dates;
        }

        var currentDate = startDate;
        var occurrenceCount = 0;

        while (true)
        {
            // Check end conditions
            if (recurrence.EndDate.HasValue && currentDate > recurrence.EndDate.Value)
                break;

            if (recurrence.MaxOccurrences.HasValue && occurrenceCount >= recurrence.MaxOccurrences.Value)
                break;

            // Check if current date should be included (not excluded)
            if (!recurrence.ExcludedDates.Any(excluded => excluded.Date == currentDate.Date))
            {
                // For weekly patterns, check if this day of week is included
                if (recurrence.Pattern == RecurrencePattern.Weekly || recurrence.Pattern == RecurrencePattern.BiWeekly)
                {
                    if (recurrence.DaysOfWeek.Contains(currentDate.DayOfWeek))
                    {
                        dates.Add(currentDate);
                        occurrenceCount++;
                    }
                }
                else if (recurrence.Pattern == RecurrencePattern.Monthly)
                {
                    // For monthly patterns, check if it matches the specified day of month
                    if (currentDate.Day == (recurrence.DayOfMonth ?? startDate.Day))
                    {
                        dates.Add(currentDate);
                        occurrenceCount++;
                    }
                }
                else
                {
                    // For daily patterns
                    dates.Add(currentDate);
                    occurrenceCount++;
                }
            }

            // Move to next occurrence
            currentDate = GetNextOccurrence(currentDate, recurrence);
        }

        return dates;
    }

    public string SerializeRecurrence(RecurrenceDetails recurrence)
    {
        return JsonSerializer.Serialize(recurrence);
    }

    public RecurrenceDetails DeserializeRecurrence(string recurrenceJson)
    {
        if (string.IsNullOrEmpty(recurrenceJson))
            return new RecurrenceDetails();

        try
        {
            return JsonSerializer.Deserialize<RecurrenceDetails>(recurrenceJson) ?? new RecurrenceDetails();
        }
        catch
        {
            return new RecurrenceDetails();
        }
    }

    public DateTime GetNextOccurrence(DateTime currentDate, RecurrenceDetails recurrence)
    {
        var interval = recurrence.Interval ?? 1;

        return recurrence.Pattern switch
        {
            RecurrencePattern.Daily => currentDate.AddDays(interval),
            RecurrencePattern.Weekly => currentDate.AddDays(interval * 7),
            RecurrencePattern.BiWeekly => currentDate.AddDays(interval * 14),
            RecurrencePattern.Monthly => currentDate.AddMonths(interval),
            RecurrencePattern.Custom => currentDate.AddDays(interval), // Default to daily for custom
            _ => currentDate.AddDays(1)
        };
    }

    public bool IsDateInRecurrence(DateTime date, RecurrenceDetails recurrence)
    {
        if (recurrence.Pattern == RecurrencePattern.None)
            return false;

        // Check if date is in excluded dates
        if (recurrence.ExcludedDates.Any(excluded => excluded.Date == date.Date))
            return false;

        // Check end date
        if (recurrence.EndDate.HasValue && date > recurrence.EndDate.Value)
            return false;

        // For weekly patterns, check day of week
        if (recurrence.Pattern == RecurrencePattern.Weekly || recurrence.Pattern == RecurrencePattern.BiWeekly)
        {
            return recurrence.DaysOfWeek.Contains(date.DayOfWeek);
        }

        // For monthly patterns, check day of month
        if (recurrence.Pattern == RecurrencePattern.Monthly)
        {
            return date.Day == (recurrence.DayOfMonth ?? 1);
        }

        return true; // For daily patterns
    }
}
