using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ETicketing.Infrastructure.Persistence;

public sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
