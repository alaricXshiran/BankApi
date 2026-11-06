using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace BankApi.Data;

// SQLite stores dates as plain text and forgets that they were UTC.
// This converter makes sure dates are saved as UTC, and marked as UTC again when read back,
// so the API returns them with a "Z" (for example 2026-01-15T09:00:00Z).
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            // Saving: convert local times to UTC; leave UTC (and unspecified) times as they are.
            value => value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value,

            // Reading: the database only holds UTC times, so label them as UTC.
            value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
    }
}