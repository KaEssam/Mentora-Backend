namespace Mentora.Core.Data;

public class RecurrenceDetails
{
    public RecurrencePattern Pattern { get; set; } = RecurrencePattern.None;

    public int? Interval { get; set; } // e.g., every 2 weeks, every 3 months

    public List<DayOfWeek> DaysOfWeek { get; set; } = new(); // For weekly patterns

    public int? DayOfMonth { get; set; } // For monthly patterns (e.g., 15th of every month)

    public DateTime? EndDate { get; set; } // When recurrence ends

    public int? MaxOccurrences { get; set; } // Maximum number of occurrences

    public List<DateTime> ExcludedDates { get; set; } = new(); // Specific dates to exclude
}
