using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ETicketing.Infrastructure.Persistence;

/// <summary>
/// Marks every DateTime read from SQL Server as UTC, so the API serialises it with a Z suffix.
/// </summary>
public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
