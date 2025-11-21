using System.Text.Json;
using Mentora.Core.Data;

namespace Mentora.Domain.Services;

public interface IRecurrenceService
{
    List<DateTime> GenerateRecurringDates(DateTime startDate, RecurrenceDetails recurrence);
    string SerializeRecurrence(RecurrenceDetails recurrence);
    RecurrenceDetails DeserializeRecurrence(string recurrenceJson);
    DateTime GetNextOccurrence(DateTime currentDate, RecurrenceDetails recurrence);
    bool IsDateInRecurrence(DateTime date, RecurrenceDetails recurrence);
}
